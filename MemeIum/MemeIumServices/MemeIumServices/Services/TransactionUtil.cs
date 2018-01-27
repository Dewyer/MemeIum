using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Models;
using MemeIumServices.Models.Transaction;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MemeIumServices.Services
{
    public interface ITransactionUtil
    {
        Transaction MakeTransaction(TransactionBody body, RSACryptoServiceProvider provider);
        Transaction GetTransactionFromForm(Wallet wallet, User user,IFormCollection form, List<InBlockTransactionVOut> voutsDesired,
            List<TransactionVOut> unspent);
        
    }

    public class TransactionUtil : ITransactionUtil
    {
        private IWalletUtil walletUtil;

        public TransactionUtil(IWalletUtil walletUtil)
        {
            this.walletUtil = walletUtil;
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

        public Transaction GetTransactionFromWallet(Wallet wallet,string msg, List<InBlockTransactionVOut> voutsDesired, List<TransactionVOut> unspent)
        {
            var rsa = walletUtil.RsaFromString(wallet.KeyString);
            return GetTransactionFromRsa(wallet.Address, msg, rsa, voutsDesired, unspent);
        }

        public Transaction GetTransactionFromRsa(string from,string msg,RSACryptoServiceProvider rsa, List<InBlockTransactionVOut> voutsDesired, List<TransactionVOut> unspent)
        {
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
                Message = msg,
                PubKey = exponentStr + " " + modulusStr,
                VInputs = vins,
                VOuts = vouts
            };
            TransactionBody.SetUniqueIdForBody(transactionBody);

            var tt = MakeTransaction(transactionBody, rsa);
            return tt;

        }

        public Transaction GetTransactionFromForm(Wallet wallet,User user,IFormCollection form,List<InBlockTransactionVOut> voutsDesired,List<TransactionVOut> unspent)
        {
            var from = "";
            if (form.ContainsKey("wallet"))
            {
                if (form["wallet"].ToString().Split('-').Length > 1)
                {
                    from = form["wallet"].ToString().Split('-')[1];
                }
            }
            if (wallet == null)
            {
                return null;
            }
            if (wallet.OwnerId != user.UId)
            {
                return null;
            }
            var rsa = walletUtil.RsaFromString(wallet.KeyString);
            return GetTransactionFromRsa(from,form["msg"].ToString(), rsa, voutsDesired, unspent);
        }


    }
}
