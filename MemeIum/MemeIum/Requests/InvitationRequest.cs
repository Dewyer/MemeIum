using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class InvitationRequest : RequestHeader
    {
        public InvitationRequest()
        {
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(InvitationRequest)];
        }

        public int DataType { get; set; }
        public string DataId { get; set; }
    }
}
