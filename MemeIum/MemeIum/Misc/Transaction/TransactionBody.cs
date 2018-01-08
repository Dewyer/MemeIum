using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MemeIum.Misc.Transaction;
using Newtonsoft.Json;

namespace MemeIum.Misc
{
    class TransactionBody
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
