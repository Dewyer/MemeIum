using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services;

namespace MemeIum.Requests
{
    class BlockRequest : RequestHeader
    {
        public BlockRequest()
        {
            Sender = RequestHeader.Me;
            Version = Configurations.Config.Version;
            Type = RequestHeader.RequestIndexes[typeof(BlockRequest)];
        }

        public Block Block;
    }
}
