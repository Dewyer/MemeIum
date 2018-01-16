using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using MemeIum.Misc;

namespace MemeIum.Services
{
    interface IP2PServer
    {
        void Start();
        void SendResponse(object response,Peer to);
        void ParseRequest(string request, Peer source);
        Queue<Action> ToParseQueue { get; set; }
    }
}
