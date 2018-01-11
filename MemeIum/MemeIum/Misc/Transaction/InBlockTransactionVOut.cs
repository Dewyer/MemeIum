using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MemeIum.Services;

namespace MemeIum.Misc.Transaction
{
    class InBlockTransactionVOut
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public int Amount { get; set; }
        public string Id { get; set; }

        public bool IsLegal()
        {
            var str = new List<string>() {FromAddress, ToAddress, Id};

            foreach (var ss in str)
            {
                if (string.IsNullOrEmpty(ss) || ss.Length >= Configurations.MAX_STR_LEN)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
