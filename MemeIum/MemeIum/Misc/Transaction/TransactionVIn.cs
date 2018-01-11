using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Misc
{
    class TransactionVIn
    {
        public string OutputId { get; set; }
        public string FromBlockId { get; set; }

        public bool IsLegal()
        {
            if (string.IsNullOrEmpty(OutputId) || string.IsNullOrEmpty(FromBlockId))
            {
                return false;
            }
            if (OutputId.Length >= 50 && FromBlockId.Length >= 50)
            {
                return false;
            }
            return true;
        }
    }
}

