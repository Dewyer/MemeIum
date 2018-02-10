using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Models;
using MemeIumServices.Models.Transaction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MemeIumServices.Services
{
    public interface INodeComService
    {
        List<Peer> Peers { get; set; }
        Transaction SendTransaction(User usr,Wallet wallet,IFormCollection form);
        void UpdatePeers();
        Task<string> ReliableRequest(string suburi);
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
        private ITransactionUtil transactionUtil;
        private string hostname = "http://memeium.azurewebsites.net";

        public NodeComService(IHostingEnvironment env,ITransactionUtil _transactionUtil)
        {
            hostingEnvironment = env;
            transactionUtil = _transactionUtil;
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
                            Amount = (long)(am* 100000f),
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

        public string SaveTransaction(Transaction transaction)
        {
            var tt = JsonConvert.SerializeObject(transaction);
            var id = DateTime.UtcNow.Ticks.ToString();;
            File.WriteAllText(hostingEnvironment.WebRootPath + $"/trans/{id}.json",tt);
            return id;
        }

        public class Ok
        {
            public bool ok { get; set; }
        }

        public void CleanUpTransactionFiles()
        {
            var files = Directory.GetFiles(hostingEnvironment.WebRootPath+"/trans/");
            foreach (var file in files)
            {
                var name = Path.GetFileName(file).Split('.')[0];
                var ticks = long.Parse(name);
                var timeOfC = new DateTime().AddTicks(ticks);

                if ((DateTime.UtcNow - timeOfC).TotalDays >= 1)
                {
                    File.Delete(file);
                }
            }
        }

        public Transaction SendTransaction(User usr,Wallet wallet,IFormCollection form)
        {
            UpdatePeers();
            var from = "";
            if (form.ContainsKey("wallet"))
            {
                if (form["wallet"].ToString().Split('-').Length > 1)
                {
                    from = form["wallet"].ToString().Split('-')[1];
                }
            }

            var target = GetVoutsFromForm(form,from);
            var unspent = GetUnspentVOutsForAddress(from);

            if (target.FindAll(r => r.Amount <= 0).Count > 0)
            {
                return null;
            }
            if (target.Sum(r => r.Amount) > unspent.Sum(r => r.Amount))
            {
                return null;
            }
            var trans = transactionUtil.GetTransactionFromForm(wallet,usr,form,target,unspent);
            if (trans == null)
            {
                return null;
            }
            CleanUpTransactionFiles();
            var id = SaveTransaction(trans);

            var resp = ReliableRequest($"api/sendtransaction/{hostname}/{id}");
            resp.Wait();

            var suc = JsonConvert.DeserializeObject<Ok>(resp.Result);
            return suc.ok ? trans : null;
        }

        public async Task<string> ReliableRequest(string suburi)
        {
            for (int ll = 0; ll < 20; ll++)
            {
                Shuffle(Peers);
                foreach (Peer peer in Peers)
                {
                    var resp = "";

                    try
                    {
                        resp = await RequestToPeer(suburi, peer);
                    }
                    catch
                    {
                        resp = "";
                    }
                    if (resp != "")
                    {
                        return resp;
                    }
                }
                await Task.Delay(100);
            }
            return "";
        }

        public void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public async Task<string> RequestToPeer(string subiro,Peer peer)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0,0,0,1,0);
                var uri = new Uri($"http://{peer.Address}:{peer.Port + 1}/{subiro}");
                var ss = await client.GetStringAsync(uri);
                return ss;
            }
        }

        public List<TransactionVOut> GetUnspentVOutsForAddress(string addr)
        {
            var resp = ReliableRequest($"api/getbalance/{addr}");
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
