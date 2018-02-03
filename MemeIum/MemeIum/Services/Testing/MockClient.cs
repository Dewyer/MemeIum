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
        private IMappingService maper;

        public MockClient()
        {
            Logger = Services.GetService<ILogger>();
            maper = Services.GetService<IMappingService>();
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

            var pp = "C:\\Users\\gerge\\Documents\\MemeIum\\Keys";
            var otherP = "C:\\Users\\gerge\\Documents\\MemeIum\\MemeIum\\MemeIum\\bin\\Debug\\netcoreapp2.0\\Keys";

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

        public void TestBigger()
        {

            var v1 = "YtNKIZBLHHkgskgT7wBPWriswmaAC7Jl+boM/+kGCg==";
            var v2 = "OWgcsK3mdY5DjNWsoegYyq7W4C/+pgcIrb4dIrulAQA=";
            var bh1 = Convert.FromBase64String(v1);
            var bh2 = Convert.FromBase64String(v2);
            var bg1 = new BigInteger(bh1);
            if (bg1 < 0)
            {
                bg1 *= -1;
            }
            var bg2 = new BigInteger(bh2);
            if (bg2 < 0)
            {
                bg2 *= -1;
            }

            Console.WriteLine(bg2 < bg1);
        }

        public void CreateNewOrigins()
        {
            var _originsFullPath = Configurations.CurrentPath + "\\BlockChain\\Data\\Origins.json";
            var origins = new List<Peer>()
            {
                new Peer()
                {
                    Address = "80.98.99.40",
                    Port = 4242
                }
            };
            File.WriteAllText(_originsFullPath,JsonConvert.SerializeObject(origins));
        }

        public string FirstTarget()
        {
            var target = BigInteger.Parse("4824670384888175101821859930073625296171707535229770506163723092099072");
            target /= 3;
            var b64 = Convert.ToBase64String(target.ToByteArray());
            return b64;
            Console.WriteLine(b64);
        }

        public void CreateGenesis()
        {
            var pp = "F:\\Projects\\MemeIum\\Keys";
            var addresses = $"{pp}\\addr.txt";

            Logger.Log("Genesis tests", 1);
            var bb = Services.GetService<IBlockChainService>();
            var ww = Services.GetService<IWalletService>();
            var miner = Services.GetService<IMinerService>();

            var tx = new List<Transaction>();

            var vouts = new List<InBlockTransactionVOut>();
            var toAddr = new List<string>(File.ReadAllLines(addresses));
            var mevout = new TransactionVOut()
            {
                Amount = 42000L *100000L,
                FromAddress = ww.Address,
                ToAddress = ww.Address
            };
            TransactionVOut.SetUniqueIdForVOut(mevout);
            vouts.Add(mevout.GetInBlockTransactionVOut());


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
                Target = FirstTarget(),
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
            Console.WriteLine(genesis.Body.Id);

        }
    }
}

