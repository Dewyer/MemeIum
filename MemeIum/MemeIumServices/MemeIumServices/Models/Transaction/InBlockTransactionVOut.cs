using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models.Transaction
{
    public class InBlockTransactionVOut
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public long Amount { get; set; }
        public string Id { get; set; }
    }
}
