using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using Newtonsoft.Json;

namespace MemeIum.Services
{
    class MappingService : IMappingService, IService
    {
        private IP2PServer _server;
        private ILogger Logger;
    
        public ObservableCollection<Peer> Peers { get; set; }
        public List<RequestForPeers> ActiveRequestForPeers;
        private string _peersFullPath;
        private string _originsFullPath;

        public Peer ThisPeer;

        public void Init()
        {
            string externalip = new WebClient().DownloadString("http://icanhazip.com");

            _peersFullPath = Configurations.CurrentPath+"\\BlockChain\\Data\\Peers.json";
            _originsFullPath = Configurations.CurrentPath + "\\BlockChain\\Data\\Origins.json";

            ThisPeer = new Peer(){Address = externalip,Port=Configurations.Config.MainPort};
            ActiveRequestForPeers = new List<RequestForPeers>();

            _server = Services.GetService<IP2PServer>();
            Logger = Services.GetService<ILogger>();

            TryLoadPeers();
            Peers.CollectionChanged += Peers_CollectionChanged;

        }

        private void Peers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            File.WriteAllText(_peersFullPath,JsonConvert.SerializeObject(Peers));
        }

        public void TryLoadPeers()
        {
            if (File.Exists(_peersFullPath))
            {
                Peers = JsonConvert.DeserializeObject<ObservableCollection<Peer>>(File.ReadAllText(_peersFullPath));
            }
            else
            {
                Peers = new ObservableCollection<Peer>();
                
                var origins = JsonConvert.DeserializeObject<List<Peer>>(File.ReadAllText(_originsFullPath));
                InitiateSweap(origins);
            }
        }

        public void InitiateSweap(List<Peer> originPeers)
        {
            Logger.Log("New Peer sweep",2);
            var request = new GetAddressesRequest();
            request.MaxPeers = Configurations.Config.MaxPeersGiven;

            foreach (var originPeer in originPeers)
            {
                _server.SendResponse(request,originPeer);

                ActiveRequestForPeers.Add(new RequestForPeers(){From=originPeer,ElapseTime = DateTime.Now.AddSeconds(Configurations.Config.SecondsToWaitForAddresses)});
            }
        }

        public void AddPeerToMe(Peer toadd)
        {
            if (Peers.ToList().FindAll(rr => rr.Equals(toadd)).Count == 0 && !ThisPeer.Equals(toadd))
            {
                Peers.Add(toadd);
                Logger.Log($"New peer: {toadd.ToString()}");
            }
        }

        public bool WantedAdresses(AddressesRequest req, Peer source)
        {
            var newActive = ActiveRequestForPeers.FindAll(r =>
                r.ElapseTime >= DateTime.Now);
            ActiveRequestForPeers = newActive.ToList();

            bool wanted = (ActiveRequestForPeers.FindAll(r =>
                               r.From.Equals(source)).Count > 0);
            if (!wanted)
            {
                Logger.Log($"Rejected Addresses From: {source.ToString()} | Many: {req.Peers.Count} | all: {ActiveRequestForPeers.Count}");
            }
            return wanted;
        }

        public void ParseAddressesRequest(AddressesRequest request,Peer source)
        {
            AddPeerToMe(source);
            if (WantedAdresses(request,source))
            {
                foreach (var peer in request.Peers)
                {
                    AddPeerToMe(peer);
                }
            }
        }

        public void ParseGetAddressesRequest(GetAddressesRequest request, Peer source)
        {
            //Answer with max n peers
            var peersRandom = new List<Peer>();
            peersRandom.AddRange(Peers);
            peersRandom.Shuffle();
            int numberOfPeersToRespond = (peersRandom.Count >= request.MaxPeers) ? request.MaxPeers : peersRandom.Count;
            var responsePeers = peersRandom.Take(numberOfPeersToRespond).ToList();
            var response = new AddressesRequest(){Peers = responsePeers };

            AddPeerToMe(source);
            _server.SendResponse(response,source);
        }

        public void Broadcast(object data)
        {
            var invit = new InvitationRequest();
            if (data.GetType() == typeof(Transaction))
            {
                invit.IsBlock = false;
            }
            else if (data.GetType() == typeof(Block))
            {
                invit.IsBlock = true;
            }
            else
            {
                return;
            }

            foreach (var peer in Peers)
            {
                _server.SendResponse(invit,peer);
            }
        }

    }
}
