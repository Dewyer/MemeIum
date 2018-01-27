using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemeIum.Misc;
using Newtonsoft.Json;

namespace MemeIum.Services.P2P
{
    class Server
    {
        public int Port { get; set; }
        public Thread ServerThread;
        private IP2PServer _server;

        public Server(IP2PServer server)
        {
            _server = server;
            ServerThread = new Thread(new ThreadStart(ServerLoop));
            ServerThread.Start();
        }


        public void ServerLoop()
        {
            var externalIp = JsonConvert.DeserializeObject<MappingService.IpJson>(new WebClient().DownloadString("https://api.ipify.org/?format=json")).Ip;
            var epmy = IPAddress.Parse(externalIp);
            Console.WriteLine("[Server]Me : "+epmy.AddressFamily.ToString());
            var ipep = new IPEndPoint(IPAddress.Any, Port);
            if (epmy.AddressFamily == AddressFamily.InterNetworkV6)
            {
                ipep = new IPEndPoint(IPAddress.IPv6Any,Port);
            }
            var client = new UdpClient(epmy.AddressFamily);
            client.Client.Bind(ipep);
            var ep = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                var rr = client.Receive(ref ep);
                var ss = Encoding.UTF8.GetString(rr);
                _server.ToParseQueue.Enqueue(()=> _server.ParseRequest(ss,Peer.FromIPEndPoint(ep)));
            }
            Console.WriteLine("[Server]Closing server..");
        }
    }
}
