﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Other;
using MemeIum.Services.Wallet;
using Newtonsoft.Json;

namespace MemeIum.Services.Mineing
{
    class MinerService : IMinerService, IService
    {
        public ObservableCollection<Transaction> MemPool { get; set; }
        public List<Thread> CurrentWorkers { get; set; }

        private IBlockChainService _blockChainService;
        private IDifficultyService _difficultyService;
        private ITransactionVerifier _transactionVerifier;
        private IEventManager _eventManager;
        private IWalletService _walletService;
        private ILogger _logger;

        private string _memPoolFullPath;

        public void Init()
        {
            _blockChainService = Services.GetService<IBlockChainService>();
            _difficultyService = Services.GetService<IDifficultyService>();
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _walletService = Services.GetService<IWalletService>();
            _logger = Services.GetService<ILogger>();

            _eventManager = Services.GetService<IEventManager>();

            _memPoolFullPath = $"{Configurations.CurrentPath}\\BlockChain\\Data\\MemPool.json";
            TryLoadMemPool();
            MemPool.CollectionChanged += MemPool_CollectionChanged;

            CurrentWorkers = new List<Thread>();
        }

        public void TryRestartingWorkers()
        {
            if (Configurations.Config.ShouldMine)
            {
                RestartMiners();
            }
        }

        private int GetCurrentBlockReward(int height)
        {
            //Halver = 131400, eq = Ma
            return (int)Math.Floor(Configurations.BLOCK_REWARD / (Math.Pow(2, Math.Floor(height / 131400f))));
        }

        private string GenerateRandomString()
        {
            string token = "";
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[16];
                rng.GetBytes(tokenData);

                token = Convert.ToBase64String(tokenData);        
            }
            return token;
        }

        public InBlockTransactionVOut GetMinerVOut(List<Transaction> transactions,int height)
        {
            var fees = transactions.Sum(CalculateFee);
            var vout = new TransactionVOut()
            {
                Amount = fees + GetCurrentBlockReward(height),
                FromAddress = _walletService.Address,
                ToAddress = _walletService.Address,
                FromBlock = "0"
            };
            TransactionVOut.SetUniqueIdForVOut(vout);
            return vout.GetInBlockTransactionVOut();
        }

        private int CalculateFee(Transaction t)
        {
            var inp = t.Body.VInputs.Sum(r => _transactionVerifier.GetUnspeTransactionVOut(r.OutputId,out bool spent).Amount);
            var outp = t.Body.VOuts.Sum(r => r.Amount);
            if (inp - outp >= 0) {
                return 0;
            } 
            return 0;
        }

        private List<Transaction> ChooseTxs()
        {
            if (MemPool.Count < 4200)
            {
                return MemPool.ToList();
            }
            else
            {
                var newPool = MemPool.OrderBy(CalculateFee).Take(4000);
                return newPool.ToList();
            }
        }

        private bool IsNounceGood(BigInteger target,Block block)
        {
            var bHash =new BigInteger( Convert.FromBase64String(block.Body.Id));
            return bHash <= target;
        }

        private void Miner()
        {
            _logger.Log("New Miner",1);
            if (MemPool.Count == 0)
            {
                return;
            }

            var nounce = GenerateRandomString();
            var newInfo = new BlockInfo()
            {
                LastBlockId = _blockChainService.Info.EndOfLongestChain,
                Height = _blockChainService.Info.Height + 1,
                Id="",
                Target = ""
            };
            var target = _difficultyService.TargetForBlock(newInfo);
            var targetString =Convert.ToBase64String( target.ToByteArray());
            var choosenTxs = ChooseTxs();

            var bBody = new BlockBody()
            {
                Height = newInfo.Height,
                Id="",
                LastBlockId = _blockChainService.Info.EndOfLongestChain,
                MinerVOut = GetMinerVOut(choosenTxs,newInfo.Height),
                Nounce = nounce,
                Target = targetString,
                Tx = choosenTxs
            };
            var block = new Block
            {
                TimeOfCreation = DateTime.UtcNow,
                Body = bBody
            };

            while (!IsNounceGood(target,block))
            {
                nounce = GenerateRandomString();
                block.TimeOfCreation = DateTime.UtcNow;
                Block.SetUniqueBlockId(block);
            }
            _logger.Log("Miner finished", 1);
            _eventManager.PassNewTrigger(block,EventTypes.EventType.NewBlock);
        }

        private void RestartMiners()
        {
            foreach (var currentWorker in CurrentWorkers)
            {
                currentWorker.Abort();
            }

            CurrentWorkers = new List<Thread>();
            for (int ii = 0; ii < Configurations.Config.MaxThreadsToMineOn; ii++)
            {
                var th = new Thread(new ThreadStart(Miner));
                th.Start();
            }
        }

        private void MemPool_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var ss = JsonConvert.SerializeObject(MemPool);
            File.WriteAllText(_memPoolFullPath,ss);

            if (Configurations.Config.ShouldMine)
            {
                RestartMiners();
            }
            
        }

        public void TryLoadMemPool()
        {
            var genNew = false;
            if (File.Exists(_memPoolFullPath))
            {
                try
                {
                    MemPool = JsonConvert.DeserializeObject<ObservableCollection<Transaction>>(File.ReadAllText(_memPoolFullPath));
                }
                catch (Exception e)
                {
                    genNew = true;
                }
            }
            else
            {
                genNew = true;
            }

            if (genNew)
            {
                MemPool = new ObservableCollection<Transaction>();
            }
        }

    }
}

