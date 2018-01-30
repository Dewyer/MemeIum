﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemeIum.Misc;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.CatchUp;
using MemeIum.Services.P2P;
using Newtonsoft.Json;

namespace MemeIum.Services
{
    class P2PServer : IP2PServer,IService
    {
        public ILogger Logger;
        private IBlockChainService _blockChainService;
        private ICatchUpService _catchUpService;

        public Queue<Action> ToParseQueue { get; set; }

        public void Init()
        {
            Logger = Services.GetService<ILogger>();
            _blockChainService = Services.GetService<IBlockChainService>();
            _catchUpService = Services.GetService<ICatchUpService>();
            ToParseQueue = new Queue<Action>();
            var parser = new Thread(new ThreadStart(ParserThread));
            parser.Start();
            Start();
        }

        public void ParserThread()
        {
            while (true)
            {
                if (ToParseQueue.Count > 0)
                {
                    var action = ToParseQueue.Dequeue();
                    action();
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        public void Start()
        {
            var server = new Server(this){Port = Configurations.Config.MainPort};
        }

        public void ParseRequest(string request,Peer source)
        {
            var header = JsonConvert.DeserializeObject<RequestHeader>(request);
            //Logger.Log(String.Format("V:{0},T:{1},D:{2}",header.Version,header.Type,request),show:true);
            source.Port = header.Sender.Port;
            Logger.Log($"Got {header.Type} {source.Port}");
            //Logger.Log($"Ketc {_catchUpService.CaughtUp}");
            if (source.Address.StartsWith("192"))
            {
                source.Address = header.Sender.Address;
            }

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
                    //Logger.Log("New data to pars");
                    _blockChainService.ParseDataRequest(req);
                }
                else
                {
                    //Logger.Log("New data to kechup");
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
                    var req = JsonConvert.DeserializeObject<DidICatchUpRequest>(request);
                    _catchUpService.ParseDidICatchUp(req,source);
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

        public void SendResponse(object response,Peer peer)
        {
            var hd = (RequestHeader)response;
            if (peer.Port == Configurations.Config.MainPort)
            {
                Logger.Log("Bad port send");
                return;
            }
            hd.Sender.Port = Configurations.Config.MainPort;
            var ep = peer.ToEndPoint();
            Logger.Log($"Sent : {hd.Type} | {peer.Address} {peer.Port} {ep.AddressFamily.ToString()} - from : {hd.Sender.Port}");
            var msg = JsonConvert.SerializeObject(response);
            var bytes = Encoding.UTF8.GetBytes(msg);
            var client = new UdpClient(ep.AddressFamily);
            if (ep.AddressFamily == AddressFamily.InterNetworkV6)
            {
                client.Client.DualMode = true;
            }
            client.Send(bytes, bytes.Length,ep);
            
        }
    }
}
