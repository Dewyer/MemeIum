using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MemeIum.Misc
{
    class BlockInfo
    {
        public string Id { get; set; }
        public string LastBlockId { get; set; }
        public DateTime CreationTime { get; set; }
        public int Height { get; set; }
        public string Target { get; set; }
    }
}
