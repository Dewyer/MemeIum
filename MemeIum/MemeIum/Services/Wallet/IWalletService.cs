using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;

namespace MemeIum.Services.Wallet
{
    interface IWalletService
    {
        string Address { get;}
        TransactionRequest MakeTransaction(TransactionBody body);
    }
}
