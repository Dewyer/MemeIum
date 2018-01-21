using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MemeIumServices.Models.Transaction;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MemeIumServices.Services
{
    public interface ITransactionUtil
    {
        Transaction MakeTransaction(TransactionBody body, RSACryptoServiceProvider provider);
        RSACryptoServiceProvider RsaFromString(string from);

        Transaction GetTransactionFromForm(IFormCollection form, List<InBlockTransactionVOut> voutsDesired,
            List<TransactionVOut> unspent);

    }

    public class TransactionUtil : ITransactionUtil
    {
        public RSAParameters RsaParametersFromString(string str)
        {
            var sr = new System.IO.StringReader(str);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            return (RSAParameters)xs.Deserialize(sr);
        }

        public RSACryptoServiceProvider RsaFromString(string from)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(RsaParametersFromString(from));

            return rsa;
        }

        public Transaction MakeTransaction(TransactionBody body, RSACryptoServiceProvider provider)
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

        public Transaction GetTransactionFromForm(IFormCollection form,List<InBlockTransactionVOut> voutsDesired,List<TransactionVOut> unspent)
        {
            var from = form["addr"].ToString();
            var privKeyString = form["privkey"].ToString();
            var rsa = RsaFromString(privKeyString);

            var exponentStr = Convert.ToBase64String(rsa.ExportParameters(false).Exponent);
            var modulusStr = Convert.ToBase64String(rsa.ExportParameters(false).Modulus);

            var vins = new List<TransactionVIn>();
            foreach (var transactionVOut in unspent)
            {
                vins.Add(new TransactionVIn() { FromBlockId = transactionVOut.FromBlock, OutputId = transactionVOut.Id });
            }
            var total = unspent.Sum(r => r.Amount);
            var vouts = new List<InBlockTransactionVOut>();
            vouts.AddRange(voutsDesired);
            var selfvout = new TransactionVOut()
            {
                Amount = total - vouts.Sum(r => r.Amount),
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
                PubKey = exponentStr + " " + modulusStr,
                VInputs = vins,
                VOuts = vouts
            };
            TransactionBody.SetUniqueIdForBody(transactionBody);

            var tt = MakeTransaction(transactionBody, rsa);
            return tt;
        }


    }
}
