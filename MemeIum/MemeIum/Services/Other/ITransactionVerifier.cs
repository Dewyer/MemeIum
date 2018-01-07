using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;

namespace MemeIum.Services.Other
{
    interface ITransactionVerifier
    {
        bool Verify(Transaction transaction);
        TransactionVOut GetUnspeTransactionVOut(string id,out bool spent);
    }
}
