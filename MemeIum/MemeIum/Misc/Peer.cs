using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Misc
{
    class Peer
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return Address+":"+Port;
        }

        public static Peer FromString(string source)
        {
            try
            {
                var tokens = source.Split(':');
                return new Peer(){Address = tokens[0],Port =int.Parse(tokens[1]) };

            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
