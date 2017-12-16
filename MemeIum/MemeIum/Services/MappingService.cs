using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;

namespace MemeIum.Services
{
    class MappingService : IMappingService
    {
        public List<Peer> Peers;

        public MappingService()
        {
            Peers = new List<Peer>();
            
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

        public void RegisterPeers(MappingRequest request)
        {
            foreach (var requestPeer in request.Peers)
            {
                Peers.Add(new Peer(){Address = requestPeer});
            }
        }
    }
}
