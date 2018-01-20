using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models.Transaction
{
    public class Transaction
    {
        public TransactionBody Body;
        public string Signature { get; set; }
    }
}
