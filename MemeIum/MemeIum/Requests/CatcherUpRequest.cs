using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class CatcherUpRequest : RequestHeader
    {
        public List<InvitationRequest> Invs { get; set; }
        public string EndOfLongestChain { get; set; }

        public CatcherUpRequest()
        {
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(DidICatchUpRequest)];
        }

    }
}
