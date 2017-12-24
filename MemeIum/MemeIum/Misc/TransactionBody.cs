using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MemeIum.Misc
{
    class TransactionBody
    {
        public string TransactionId { get; set; }
        public string FromAddress { get; set; }
        public float VInput { get; set; }

        public List<TransactionVOut> VOuts { get; set; }

        public static void SetUniqueIdForBody(TransactionBody body)
        {
            body.TransactionId = "42";
            var json = JsonConvert.SerializeObject(body);
            json += DateTime.Now.Ticks.ToString();
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(json));

            body.TransactionId = Convert.ToBase64String(hash);
        }
    }
}
