using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using MemeIum.Services.CatchUp;
using Newtonsoft.Json;
using Open.Nat;

namespace MemeIum.Services
{
    class MappingService : IMappingService, IService
    {
        private IP2PServer _server;
        private ILogger Logger;
        private ICatchUpService _catcherUp;

        public ObservableCollection<Peer> Peers { get; set; }
        public List<RequestForPeers> ActiveRequestForPeers;
        private string _peersFullPath;
        private string _originsFullPath;

        public Peer ThisPeer { get; set; }
        private string _trackerIp = "http://memeiumtracker.azurewebsites.net/track";
        private string _trackerSignUp = "http://memeiumtracker.azurewebsites.net/track/set?";
        private string externalIp;

        public class IpJson
        {
            public string Ip { get; set; }
        }

        public void Init()
        {
            externalIp = JsonConvert.DeserializeObject<IpJson>(new WebClient().DownloadString("https://api.ipify.org/?format=json")).Ip;

            _peersFullPath = Configurations.CurrentPath+"\\BlockChain\\Data\\Peers.json";
            _originsFullPath = Configurations.CurrentPath + "\\BlockChain\\Data\\Origins.json";

            ThisPeer = new Peer(){Address = externalIp, Port=Configurations.Config.MainPort};
            RequestHeader.Me = ThisPeer;

            ActiveRequestForPeers = new List<RequestForPeers>();

            _server = Services.GetService<IP2PServer>();
            _catcherUp = Services.GetService<ICatchUpService>();

            Logger = Services.GetService<ILogger>();


            TryLoadPeers();
            Task.Run(() => PeerUpdateLoop());

            Peers.CollectionChanged += Peers_CollectionChanged;

            try
            {
                DoUPnP().Wait();
                Logger.Log("NAT traversal successfull.");
            }
            catch (Exception ex)
            {
                Logger.Log("UPnP NAT traversal error: "+ex.Message+" || "+ex.StackTrace,2);
            }
        }

        public async Task DoUPnP()
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource(10000);
            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            await device.CreatePortMapAsync(new Mapping(Protocol.Udp, Configurations.Config.MainPort, Configurations.Config.MainPort, "Main UDP#1"));
            await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, Configurations.Config.MainPort+1, Configurations.Config.MainPort+1, "Main HTTP#1"));
        }

        public void RegisterPeer(Peer peer)
        {
            if (peer.Port != Configurations.Config.MainPort)
            {
                Peers.Add(peer);
            }
        }

        private async void TrySignUpToTracker()
        {
            try
            {
                var client = new HttpClient();
                var myurl = _trackerSignUp + "ip=" + ThisPeer.Address + "|" + ThisPeer.Port;
                var resp = await client.GetStringAsync(new Uri(myurl));
                Logger.Log($"Signup : {resp}", show: false, saveInfo: true);
            }
            catch (Exception e)
            {
                Logger.Log("Can't sign up for tracker.", 2);
            }

        }

        public void PeerUpdateLoop()
        {
            var runs = 0;
            while (true)
            {
                TryTracker();
                TrySignUpToTracker();
                Task.Delay(2000).Wait();
                if (runs == 3)
                {
                    _catcherUp.StartCatchup();
                }
                runs++;
            }
        }

        private async void TryTracker()
        {
            try
            {
                var client = new HttpClient();
                var resp = await client.GetStringAsync(new Uri(_trackerIp));

                var peers = JsonConvert.DeserializeObject<List<Peer>>(resp);
                var oldPeers = new List<Peer>();
                oldPeers.AddRange(Peers);
                Logger.Log($"New track : {resp}",show:false,saveInfo:true);
                if (peers.Count != 0)
                {
                    foreach (var peer in peers)
                    {
                        var inalredy = oldPeers.ToList().FindAll(r => r.Address == peer.Address && r.Port == peer.Port)
                                           .Count > 0;

                        if (!inalredy&&(peer.Address != ThisPeer.Address || peer.Port != ThisPeer.Port))
                        {
                            RegisterPeer(peer);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Tracker is not reachable",2);
            }
        }

        private void Peers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //File.WriteAllText(_peersFullPath,JsonConvert.SerializeObject(Peers));
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
                
                //var origins = JsonConvert.DeserializeObject<List<Peer>>(File.ReadAllText(_originsFullPath));
                //InitiateSweap(origins);
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
                RegisterPeer(toadd);
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

        public void Broadcast<T>(T data)
        {
            //TrySignUpToTracker();

            var invit = new InvitationRequest();
            var tdata = data as Transaction;
            var bdata = data as Block;
            if (tdata != null)
            {
                var tt = tdata;
                invit.IsBlock = false;
                invit.DataId = tt.Body.TransactionId;
            }
            else if (bdata != null)
            {
                var bb = bdata;
                invit.IsBlock = true;
                invit.DataId = bb.Body.Id;
            }
            else
            {
                Logger.Log("bad type bad shit");
                return;
            }
            //Logger.Log($"Broadcasting : {invit.DataId} {invit.IsBlock}");

            foreach (var peer in Peers)
            {
                _server.SendResponse(invit,peer);
            }
        }

    }
}
