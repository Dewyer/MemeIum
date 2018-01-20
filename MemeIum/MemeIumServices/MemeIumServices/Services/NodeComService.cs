using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MemeIumServices.Models.Transaction;
using Newtonsoft.Json;

namespace MemeIumServices.Services
{
    public interface INodeComService
    {
        object SendTransaction(Transaction transaction);
    }

    public class Peer
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }

    public class NodeComService : INodeComService
    {
        public List<Peer> Peers;

        public NodeComService()
        {
            Peers = new List<Peer>();
        }

        public async Task UpdatePeers()
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri("http://memeiumtracker.azurewebsites.net/track");
                var ss = await client.GetStringAsync(uri);
                Peers = JsonConvert.DeserializeObject<List<Peer>>(ss);
            }
        }

        public object SendTransaction(Transaction transaction)
        {
            UpdatePeers().Wait();

            return new {outp=Peers.Count };
        }
    }
}
