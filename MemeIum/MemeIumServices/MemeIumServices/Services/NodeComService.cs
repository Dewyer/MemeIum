using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MemeIumServices.Models.Transaction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MemeIumServices.Services
{
    public interface INodeComService
    {
        List<Peer> Peers { get; set; }
        object SendTransaction(Transaction transaction);
        Transaction GetTransactionFromForm(IFormCollection form);
        void UpdatePeers();
        Task<string> RequestToRandomPeer(string subiro);
        List<TransactionVOut> GetUnspentVOutsForAddress(string addr);
    }

    public class Peer
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }

    public class NodeComService : INodeComService
    {
        public List<Peer> Peers { get; set; }
        private Random rng;
        private IHostingEnvironment hostingEnvironment;

        public NodeComService(IHostingEnvironment env)
        {
            hostingEnvironment = env;
            Peers = new List<Peer>();
            rng = new Random();
        }

        public void UpdatePeers()
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri("http://memeiumtracker.azurewebsites.net/track");
                var ss = client.GetStringAsync(uri);
                ss.Wait();
                Peers = JsonConvert.DeserializeObject<List<Peer>>(ss.Result);
            }
        }

        public RSAParameters RsaParametersFromString(string str)
        {
            var sr = new System.IO.StringReader(str);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            return (RSAParameters)xs.Deserialize(sr);
        }

        public List<InBlockTransactionVOut> GetVoutsFromForm(IFormCollection form,string fromaddr)
        {
            var outV = new List<InBlockTransactionVOut>();

            for (int ii = 0; ii < 100; ii++)
            {
                if (form.ContainsKey($"toaddr{ii}"))
                {
                    try
                    {
                        var am = float.Parse(form[$"ammount{ii}"]);
                        var to = form[$"toaddr{ii}"];
                        var vout = new TransactionVOut()
                        {
                            Amount = (int)(am* 100000f),
                            FromAddress = fromaddr,
                            ToAddress = to,
                            FromBlock = ""
                        };
                        TransactionVOut.SetUniqueIdForVOut(vout);
                        outV.Add(vout.GetInBlockTransactionVOut());
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return outV;
        }

        public Transaction MakeTransaction(TransactionBody body,RSACryptoServiceProvider provider)
        {
            var bod = JsonConvert.SerializeObject(body);
            var bbytes = Encoding.UTF8.GetBytes(bod);
            var sign = provider.SignData(bbytes, new SHA256CryptoServiceProvider());
            var signString = Convert.ToBase64String(sign);

            var req = new Transaction()
            {
                Body = body,
                Signature = signString
            };

            return req;
        }

        public Transaction GetTransactionFromForm(IFormCollection form)
        {
            var from = form["addr"].ToString();
            var privKeyString = form["privkey"].ToString();
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(RsaParametersFromString(privKeyString));
            var exponentStr = Convert.ToBase64String(rsa.ExportParameters(false).Exponent);
            var modulusStr = Convert.ToBase64String(rsa.ExportParameters(false).Modulus);

            var unspent = GetUnspentVOutsForAddress(from);
            var vins = new List<TransactionVIn>();
            foreach (var transactionVOut in unspent)
            {
                vins.Add(new TransactionVIn(){FromBlockId = transactionVOut.FromBlock,OutputId = transactionVOut.Id});
            }
            var total = unspent.Sum(r => r.Amount);
            var vouts = GetVoutsFromForm(form,from);
            var selfvout = new TransactionVOut()
            {
                Amount = total-vouts.Sum(r=>r.Amount),
                FromAddress = from,
                ToAddress = from,
                FromBlock = ""
            };
            TransactionVOut.SetUniqueIdForVOut(selfvout);
            vouts.Add(selfvout.GetInBlockTransactionVOut());

            var transactionBody = new TransactionBody()
            {
                FromAddress = from,
                Message = form["msg"].ToString(),
                PubKey = exponentStr+" "+modulusStr,
                VInputs = vins,
                VOuts = vouts
            };
            TransactionBody.SetUniqueIdForBody(transactionBody);

            var tt = MakeTransaction(transactionBody, rsa);
            return tt;
        }

        public string SaveTransaction(Transaction transaction)
        {
            var tt = JsonConvert.SerializeObject(transaction);
            var id = DateTime.UtcNow.Ticks.ToString();;
            File.WriteAllText(hostingEnvironment.WebRootPath + $"/trans/{id}.json",tt);
            return id;
        }

        public object SendTransaction(Transaction transaction)
        {
            UpdatePeers();
            var id = SaveTransaction(transaction);

            var resp = RequestToRandomPeer($"api/sendtransaction/localhost:53479/{id}");
            resp.Wait();
            
            return resp.Result;
        }

        public async Task<string> RequestToRandomPeer(string subiro)
        {
            try
            {
                //var randomPeer = Peers[rng.Next(0, Peers.Count)];
                using (var client = new HttpClient())
                {
                    var uri = new Uri($"http://localhost:4243/{subiro}");
                    var ss = await client.GetStringAsync(uri);
                    return ss;
                }
            }
            catch (Exception ex)
            {
                return ex.Message +"  "+ex.StackTrace;
            }
        }

        public List<TransactionVOut> GetUnspentVOutsForAddress(string addr)
        {
            var resp =RequestToRandomPeer($"api/getbalance/{addr}");
            resp.Wait();
            if (resp.Result == "")
            {
                return null;
            }
            //throw new Exception(resp.Result);
            var vouts = JsonConvert.DeserializeObject<List<TransactionVOut>>(resp.Result);
            return vouts;
        }
    }
}
