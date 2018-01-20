using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MemeIumServices.Models.Transaction
{
    public class TransactionVOut
    {
        public string FromBlock { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public int Amount { get; set; }
        public string Id { get; set; }

        public InBlockTransactionVOut GetInBlockTransactionVOut()
        {
            return new InBlockTransactionVOut()
            {
                FromAddress = this.FromAddress,
                ToAddress = this.ToAddress,
                Amount = this.Amount,
                Id = this.Id
            };
        }

        public static void SetUniqueIdForVOut(TransactionVOut vout)
        {
            vout.Id = "42";
            var ss = JsonConvert.SerializeObject(vout) + DateTime.UtcNow.Ticks.ToString();
            var hash = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ss)));
            vout.Id = hash;
        }


    }
}
