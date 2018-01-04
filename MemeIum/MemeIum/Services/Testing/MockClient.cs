using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;
using MemeIum.Services.Wallet;
using Newtonsoft.Json;

namespace MemeIum.Services
{
    class MockClient
    {
        private ILogger Logger;

        public MockClient()
        {
            Logger = Services.GetService<ILogger>();
        }

        public void MockTestMapp()
        {
            Logger.Log("Running tests",1);
            var loch = "127.0.0.1";
            var mapper = Services.GetService<IMappingService>();

            var origins = new List<Peer> {new Peer() {Address = loch, Port = 3232}, new Peer() { Address = loch, Port = 3233 } };
            var second = new List<Peer> {new Peer() {Address = loch, Port = 4242}};
            if (Configurations.Config.MainPort == 4242)
            {
                mapper.InitiateSweap(origins);
            }
            else
            {
                mapper.InitiateSweap(second);
            }
        }

        public void MockTest()
        {
            Logger.Log("Running tests", 1);
            var wallet = Services.GetService<IWalletService>();
            var body = new TransactionBody()
            {
                FromAddress = wallet.Address,
                VInputs = null,
                VOuts = new List<TransactionVOut>() { new TransactionVOut(){ToAddress = "asd",VOut = 1}}
            };
            TransactionBody.SetUniqueIdForBody(body);
            
            var trs = wallet.MakeTransaction(body);

            Logger.Log(JsonConvert.SerializeObject(trs));
            //Logger.Log(trs.Signature);
        }
    }
}
