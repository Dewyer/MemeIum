using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using MemeIum.Requests;

namespace MemeIum.Misc
{
    class BlockBody
    {
        public int Height { get; set; }
        public string LastBlockId { get; set; }
        public string Id { get; set; }
        public TransactionVOut MinerVOut { get; set; }

        public string Target { get; set; }
        public string Nounce { get; set; }

        public List<Transaction.Transaction> Tx { get; set; }

    }
}
