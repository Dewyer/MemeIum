using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class InvitationResponseRequest : RequestHeader
    {
        public bool IsBlock { get; set; }
        public string WantedDataId { get; set; }

        public InvitationResponseRequest()
        {
            Sender = RequestHeader.Me;
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(InvitationResponseRequest)];
        }

    }
}
