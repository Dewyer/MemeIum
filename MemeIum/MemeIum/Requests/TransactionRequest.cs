using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class TransactionRequest : RequestHeader
    {
        public TransactionBody Body { get; set; }
        public string Signature { get; set; }

        public TransactionRequest()
        {
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(TransactionRequest)];
        }
    }
}
