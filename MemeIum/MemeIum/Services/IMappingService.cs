using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MemeIum.Requests;

namespace MemeIum.Services
{
    interface IMappingService
    {
        void InitiateSweap(List<string> originPeers);
        void ParsePeerRequest(MappingRequest request,IPEndPoint source);
    }
}
