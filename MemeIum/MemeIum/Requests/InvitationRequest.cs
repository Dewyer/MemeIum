using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class InvitationRequest : RequestHeader
    {
        public InvitationRequest()
        {
            Sender = RequestHeader.Me;
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(InvitationRequest)];
        }

        public bool IsBlock { get; set; }
        public string DataId { get; set; }
    }
}
