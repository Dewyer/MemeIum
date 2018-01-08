using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Misc.Transaction
{
    class InBlockTransactionVOut
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public int Amount { get; set; }
        public string Id { get; set; }
    }
}
