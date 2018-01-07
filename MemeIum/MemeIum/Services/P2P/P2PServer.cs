using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MemeIum.Misc;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.CatchUp;
using Newtonsoft.Json;

namespace MemeIum.Services
{
    class P2PServer : IP2PServer,IService
    {
        public ILogger Logger;
        private IBlockChainService _blockChainService;
        private ICatchUpService _catchUpService;

        private UdpClient socket;

        public void Init()
        {
            Logger = Services.GetService<ILogger>();
            _blockChainService = Services.GetService<IBlockChainService>();
            _catchUpService = Services.GetService<ICatchUpService>();
            Start();
        }

        public void Start()
        {
            socket = new UdpClient(Configurations.Config.MainPort);

            socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
        }

        void OnUdpData(IAsyncResult result)
        {
            socket = result.AsyncState as UdpClient;
            IPEndPoint source = new IPEndPoint(0, 0);

            byte[] message = socket.EndReceive(result, ref source);
            var msgStr = Encoding.UTF8.GetString(message);
            Logger.Log(String.Format("Msg: {0}, From : {1}", msgStr, source.Address.ToString()));
            var sourcePeer = Peer.FromIPEndPoint(source);

            try
            {
                ParseRequest(msgStr, sourcePeer);
            }
            catch (Exception e)
            {
                Logger.Log(String.Format("Caught While parsing: {0}",e.ToString()));
            }
            
            socket.BeginReceive(new AsyncCallback(OnUdpData), socket);

        }

        private void ParseRequest(string request,Peer source)
        {
            var header = JsonConvert.DeserializeObject<RequestHeader>(request);
            Logger.Log(String.Format("V:{0},T:{1}",header.Version,header.Type));

            var mapper = Services.GetService<IMappingService>();

            if (header.Type == 0)
            {
                var req = JsonConvert.DeserializeObject<GetAddressesRequest>(request);
                mapper.ParseGetAddressesRequest(req,source);
            }
            else if (header.Type == 1)
            {
                var req = JsonConvert.DeserializeObject<AddressesRequest>(request);
                mapper.ParseAddressesRequest(req,source);
            }
            else if (header.Type == 2)
            {
                var req = JsonConvert.DeserializeObject<InvitationRequest>(request);
                if (_catchUpService.CaughtUp)
                {
                    _blockChainService.ParseInvitationRequest(req, source);
                }
            }
            else if (header.Type == 3)
            {
                var req = JsonConvert.DeserializeObject<InvitationResponseRequest>(request);
                if (_catchUpService.CaughtUp)
                {
                    _blockChainService.ParseInvitationResponseRequest(req, source);
                }
            }
            else if (header.Type == 4)
            {
                var req = JsonConvert.DeserializeObject<TransactionRequest>(request);
                if (_catchUpService.CaughtUp)
                {
                    _blockChainService.ParseDataRequest(req);
                }
                else
                {
                    _catchUpService.ParseCatchUpData(req);
                }
            }
            else if (header.Type == 5)
            {
                var req = JsonConvert.DeserializeObject<BlockRequest>(request);
                if (_catchUpService.CaughtUp)
                {
                    _blockChainService.ParseDataRequest(req);
                }
                else
                {
                    _catchUpService.ParseCatchUpData(req);
                }
            }
            else if (header.Type == 6)
            {
                if (_catchUpService.CaughtUp)
                {

                }
            }
            else if (header.Type == 7)
            {
                if (!_catchUpService.CaughtUp)
                {
                    var req = JsonConvert.DeserializeObject<CatcherUpRequest>(request);
                    _catchUpService.ParseCatcherUpRequest(req,source);
                }
            }

        }

        public void SendResponse<T>(T response,Peer peer)
        {
            var respString = JsonConvert.SerializeObject(response);
            var bytes = Encoding.UTF8.GetBytes(respString);
            socket.Send(bytes,bytes.Length,peer.ToEndPoint());
        }
    }
}
