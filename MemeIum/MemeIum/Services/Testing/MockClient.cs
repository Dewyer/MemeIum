using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
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

        }

        public void MockTest1()
        {
            Logger.Log("Running tests", 1);
            var bb = Services.GetService<IBlockChainService>();
            var ww = Services.GetService<IWalletService>();

            var genesis = new Block();
            genesis.TimeOfCreation = DateTime.UtcNow;
            genesis.Body = new BlockBody();
            genesis.Body.Height = 0;
            genesis.Body.LastBlockId = "0";
            genesis.Body.Target = 0;
            genesis.Body.Nounce = "42";
            genesis.Body.Tx = new List<Transaction>();
            Block.SetUniqueBlockId(genesis);
            var bb1 = new TransactionBody(){FromAddress = "+Je/HCsw1/oOmTUP+OqzQSrgC/mJc+Pe1UyYLKvE4wU=",
                Message = "Initial coin offer.",
                PubKey = ww.PubKey,
                VInputs = null,
                VOuts = new List<TransactionVOut>()
                {
                    //new TransactionVOut(){ToAddress = "mXTDa61AImRC8wrs1HrKItGakDAoRLVXuVhqkXoMZK8",VOut = 100},
                    //new TransactionVOut(){ToAddress = "iFFN2KYcKWGcquJHFZHlkiJySiuli+Lhc5K7+mePyeA=",VOut = 100},

                }
            };
            var tt = ww.MakeTransaction(bb1);
            genesis.Body.Tx.Add(tt);
            //+Je/HCsw1/oOmTUP+OqzQSrgC/mJc+Pe1UyYLKvE4wU= - me
            //mXTDa61AImRC8wrs1HrKItGakDAoRLVXuVhqkXoMZK8= - Bianka
            //iFFN2KYcKWGcquJHFZHlkiJySiuli+Lhc5K7+mePyeA= - Gembela
            
            bb.SaveBlock(genesis);
            Console.WriteLine("done");

        }
    }
}
