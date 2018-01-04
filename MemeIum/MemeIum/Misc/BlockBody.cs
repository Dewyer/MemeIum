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
        public string MinedByAddress { get; set; }
        public float Target { get; set; }
        public string Nounce { get; set; }

        public List<TransactionRequest> Tx { get; set; }

    }
}
