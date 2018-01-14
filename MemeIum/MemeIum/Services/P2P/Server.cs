using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MemeIum.Misc;

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
            var externalip = new WebClient().DownloadString("http://icanhazip.com").Split('\n',' ')[0];
            var epmy = IPAddress.Parse(externalip);
            Console.WriteLine("[Server]Me : "+epmy.AddressFamily.ToString());
            var ipep = new IPEndPoint(IPAddress.Any, Port);
            var client = new UdpClient(epmy.AddressFamily);
            client.Client.Bind(ipep);
            var ep = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                var rr = client.Receive(ref ep);
                var ss = Encoding.UTF8.GetString(rr);
                _server.ParseRequest(ss,Peer.FromIPEndPoint(ep));
            }
            Console.WriteLine("[Server]Closing server..");
        }
    }
}
