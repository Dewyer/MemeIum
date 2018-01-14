using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class GetAddressesRequest : RequestHeader
    {
        public GetAddressesRequest()
        {
            Sender = RequestHeader.Me;
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(GetAddressesRequest)];
        }

        public int MaxPeers { get; set; }
    }

}
