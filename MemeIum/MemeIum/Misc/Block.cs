using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MemeIum.Services;
using Newtonsoft.Json;

namespace MemeIum.Misc
{
    class Block
    {
        public DateTime TimeOfCreation { get; set; }
        public BlockBody Body { get; set; }

        public static void SetUniqueBlockId(Block block)
        {
            block.Body.Id = "42";
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(block)));
            block.Body.Id = Convert.ToBase64String(hash);
        }

        public bool IsLegal()
        {
            var strs = new List<string>() { Body.Id,Body.LastBlockId,Body.Nounce,Body.Target};

            foreach (var str in strs)
            {
                if (string.IsNullOrEmpty(str) || str.Length > Configurations.MAX_STR_LEN)
                {
                    return false;
                }
            }

            if (Body.MinerVOut == null || Body.Tx == null)
            {
                return false;
            }

            return true;
        }
    }
}
