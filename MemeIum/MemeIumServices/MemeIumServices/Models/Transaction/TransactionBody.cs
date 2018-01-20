using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}