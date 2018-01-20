using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MemeIumServices.Models.Transaction
{
    public class TransactionBody
    {
        public string TransactionId { get; set; }
        public string FromAddress { get; set; }
        public string PubKey { get; set; }
        public string Message { get; set; }
        public List<TransactionVIn> VInputs { get; set; }

        public List<InBlockTransactionVOut> VOuts { get; set; }

        public static void SetUniqueIdForBody(TransactionBody body)
        {
            body.TransactionId = "42";
            var json = JsonConvert.SerializeObject(body);
            json += DateTime.UtcNow.Ticks.ToString();
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(json));

            body.TransactionId = Convert.ToBase64String(hash);
        }
    }
}