using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class GetAddressesRequest : RequestHeader
    {
        public GetAddressesRequest()
        {
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(GetAddressesRequest)];
        }

        public int MaxPeers { get; set; }
    }

}
