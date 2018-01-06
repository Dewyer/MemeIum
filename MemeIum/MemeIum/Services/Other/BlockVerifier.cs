using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;

namespace MemeIum.Services.Other
{
    class BlockVerifier : IBlockVerifier
    {
        private readonly ITransactionVerifier _transactionVerifier;

        public BlockVerifier()
        {
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
        }

        public bool Verify(Block block)
        {
            return false;
        }
    }
}
