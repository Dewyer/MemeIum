using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MemeIum.Misc
{
    class TransactionVOut
    {
        public string FromBlock { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public float Amount { get; set; }
        public string Id { get; set; }

        public static void SetUniqueIdForVOut(TransactionVOut vout)
        {
            vout.Id = "42";
            var ss = JsonConvert.SerializeObject(vout)+DateTime.Now.Ticks.ToString();
            var hash = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ss)));
            vout.Id = hash;
        }
    }
}
