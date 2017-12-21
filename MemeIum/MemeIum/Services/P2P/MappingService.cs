using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;

namespace MemeIum.Services
{
    class MappingService : IMappingService
    {
        public List<Peer> Peers;
        public Peer ThisPeer;

        public MappingService()
        {
            string externalip = new WebClient().DownloadString("http://icanhazip.com");

            Peers = new List<Peer>();
            ThisPeer = new Peer(){Address = externalip};
        }

        public void InitiateSweap(List<string> originPeers)
        {
            
        }

        public void ParsePeerRequest(MappingRequest request,IPEndPoint source)
        {
            Console.WriteLine("Parsin: {0}",source.Address);

            if (request.Ask)
            {

            }
            else
            {
                RegisterPeers(request);
            }
        }

        public void AnswerToAsk(MappingRequest request,IPEndPoint source)
        {
            var socket = new UdpClient();
            var peersToSend = new List<string>();
            
            foreach (var peer in Peers)
            {
                if (request.Peers.FindAll(r => r == peer.ToString()).Count == 0)
                {
                    peersToSend.Add(peer.ToString());
                }
            }
        }

        public void RegisterPeers(MappingRequest request)
        {
            foreach (var requestPeer in request.Peers)
            {
                if (Peers.FindAll(r => r.ToString() == requestPeer).Count == 0)
                {
                    Peers.Add(Peer.FromString(requestPeer));
                }
            }
        }
    }
}
