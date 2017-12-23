using System;
using System.Collections.Generic;
using System.Net;
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

        public bool Equals(Peer to)
        {
            return to.Address == Address && to.Port == Port;
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

        public static Peer FromIPEndPoint(IPEndPoint endp)
        {
            return new Peer(){Address = endp.Address.ToString(),Port = endp.Port};
        }

        public IPEndPoint ToEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(this.Address),Port );
        }
    }
}
