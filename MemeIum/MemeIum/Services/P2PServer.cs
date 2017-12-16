using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MemeIum.Requests;
using Newtonsoft.Json;

namespace MemeIum.Services
{
    class P2PServer : IP2PServer
    {

        public void Start()
        {
            var socket = new UdpClient(Configurations.Config.MainPort);

            socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
        }

        void OnUdpData(IAsyncResult result)
        {
            UdpClient socket = result.AsyncState as UdpClient;
            IPEndPoint source = new IPEndPoint(0, 0);

            byte[] message = socket.EndReceive(result, ref source);
            var msgStr = Encoding.UTF8.GetString(message);
            Console.WriteLine("Msg: {0}, From : {1}", msgStr, source.Address.ToString());

            try
            {
                ParseRequest(msgStr,source);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught While parsing: {0}",e.ToString());
            }

            socket.BeginReceive(new AsyncCallback(OnUdpData), socket);

        }

        private void ParseRequest(string request,IPEndPoint source)
        {
            var header = JsonConvert.DeserializeObject<RequestHeader>(request);
            Console.WriteLine("V:{0},T:{1}",header.Version,header.Type);

            if (header.Type == 0)
            {
                var req = JsonConvert.DeserializeObject<MappingRequest>(request);
                var mapper = Services.GetService<IMappingService>();
                mapper.ParsePeerRequest(req,source);
            }
        }
    }
}
