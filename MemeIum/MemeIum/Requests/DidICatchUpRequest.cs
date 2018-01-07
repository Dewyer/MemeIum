using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class DidICatchUpRequest : RequestHeader
    {
        public DateTime LastOnline { get; set; }
        public string LastKnownEnd { get; set; }

        public DidICatchUpRequest()
        {
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(DidICatchUpRequest)];
        }
    }
}
