using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Requests
{
    class MappingRequest : RequestHeader
    {
        public bool Ask { get; set; }
        public string AskFor { get; set; }

        public List<string> Peers { get; set; }
        public List<string> AskedPeers { get; set; }
    }

}
