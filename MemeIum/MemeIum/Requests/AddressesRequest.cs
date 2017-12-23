using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class AddressesRequest : RequestHeader
    {
        public AddressesRequest()
        {
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(AddressesRequest)];
        }

        public List<Peer> Peers { get; set; }
    }
}
