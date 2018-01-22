using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;

namespace MemeIum.Services.Wallet
{
    interface IWalletService
    {
        string Address { get;}
        string PubKey { get; }
        Transaction MakeTransaction(TransactionBody body);
        void TryGenerateingNewkeys();
        Transaction AssembleTransaction(string to, float ammount, string msg);
    }
}
