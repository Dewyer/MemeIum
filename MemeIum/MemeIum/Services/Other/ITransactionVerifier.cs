using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Requests;

namespace MemeIum.Services.Other
{
    interface ITransactionVerifier
    {
        bool Verify(TransactionRequest transaction);
    }
}
