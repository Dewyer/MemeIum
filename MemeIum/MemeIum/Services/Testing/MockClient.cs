using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Mineing;
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

        public void GenerateRandomWallets(int number)
        {
            var wallet = Services.GetService<IWalletService>();

            var pp = "F:\\Projects\\MemeIum\\Keys\\";
            var otherP = "F:\\Projects\\MemeIum\\MemeIum\\MemeIum\\bin\\Debug\\netcoreapp2.0\\Keys";

            var addresses = $"{pp}\\addr.txt";
            var addrs = new List<string>();

            for (int ii = 0; ii < number; ii++)
            {
                wallet.TryGenerateingNewkeys();
                //copy it
                addrs.Add(wallet.Address);
                Console.WriteLine(wallet.Address);
                Directory.Move(otherP, $"{pp}{ii}");
                Thread.Sleep(100);
            }

            File.WriteAllLines(addresses,addrs.ToArray());

        }

        public void FirstTarget()
        {
            var max = BigInteger.Pow(2, 256);
            //want 2000 hashes to run
            //chances to roll good = (max-target)/max * 2000 = 1, or 1999x / 2000 = y
            var target = BigInteger.Multiply(1999, max);
            target = BigInteger.Divide(target, 2000);
            var maxVaStr = Convert.ToBase64String(target.ToByteArray());

            Console.WriteLine(maxVaStr);
        }

        public void CreateGenesis()
        {
            var pp = "F:\\Projects\\MemeIum\\Keys\\";
            var addresses = $"{pp}\\addr.txt";

            Logger.Log("Genesis tests", 1);
            var bb = Services.GetService<IBlockChainService>();
            var ww = Services.GetService<IWalletService>();
            var miner = Services.GetService<IMinerService>();

            var tx = new List<Transaction>();

            var vouts = new List<InBlockTransactionVOut>();
            var toAddr = new List<string>(File.ReadAllLines(addresses));
            foreach (var addr in toAddr)
            {
                var vout = new InBlockTransactionVOut()
                {
                    Amount = 10000,
                    FromAddress = ww.Address,
                    ToAddress = addr
                };
                vouts.Add(vout);
            }

            var tBody = new TransactionBody()
            {
                FromAddress = ww.Address,
                Message = "Genesis block memes",
                PubKey = ww.PubKey,
                VInputs = new List<TransactionVIn>(),
                VOuts = vouts
            };

            tx.Add(ww.MakeTransaction(tBody));
            var body = new BlockBody()
            {
                Height = 0,
                LastBlockId = "0",
                MinerVOut = miner.GetMinerVOut(tx,0),
                Nounce = "42",
                Target = "rBxaZDvfT42XbhKDwMqhRbbz/dR46SYxCKwcWmQ73/8A",
                Tx=tx
                
            };

            var genesis = new Block()
            {
                Body = body,
                TimeOfCreation = DateTime.UtcNow
            };
            Block.SetUniqueBlockId(genesis);

            bb.SaveBlock(genesis);
            Console.WriteLine("done");

        }
    }
}
