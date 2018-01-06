using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;

namespace MemeIum.Services.Other
{
    interface ITransactionVerifier
    {
        bool Verify(Transaction transaction);
    }
}
