using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public static Dictionary<int, bool> Terminators;
        public static int atId = 0;

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
            Terminators = new Dictionary<int, bool>();

            return;
            Task.Run(delegate
            {
                _logger.Log("Starting first miner in 6..");
                Thread.Sleep(3000);
                _logger.Log("Starting first miner in 3..");
                Thread.Sleep(3000);
                TryRestartingWorkers();                
            });
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

        public bool HasTransactionInMemPool(string transactionId)
        {
            if (MemPool.ToList().FindAll(r => r.Body.TransactionId == transactionId)
                    .Count > 0)
            {
                return true;
            }
            return false;
        }

        private int CalculateFee(Transaction t)
        {
            var inp = t.Body.VInputs.Sum(r => _transactionVerifier.GetUnspeTransactionVOut(r.OutputId,out bool spent).Amount);
            var outp = t.Body.VOuts.Sum(r => r.Amount);
            if (inp - outp >= 0) {
                return inp - outp;
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
            var bytes = Convert.FromBase64String(block.Body.Id);
            var bHash =new BigInteger( bytes);
            if (bHash < 0)
            {
                bHash *= -1;
            }
            return bHash <= target;
        }

        private List<Transaction> SelfTrans()
        {
            var tt = _walletService.AssembleTransaction(_walletService.Address, 1, "generate mims");
            if (tt != null)
                return new List<Transaction>(){tt};
            return new List<Transaction>();
        }

        private void Miner()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine("[Info]New miner {0}",id);
            if (Terminators.ContainsKey(id))
            {
                Terminators[id] = false;
            }
            else
            {
                Terminators.Add(id, false);
            }
            List<Transaction> choosenTxs;
            if (MemPool.Count == 0)
            {
                return;
            }
            else
            {
                choosenTxs = ChooseTxs();
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
            _logger.Log($"Choosen for mine : {choosenTxs.Count}");
            foreach (var transaction in choosenTxs)
            {
                _logger.Log($"{transaction.Body.TransactionId} - transaction");
            }

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
            var lastTime = DateTime.UtcNow;
            var tries = 0;
            Block.SetUniqueBlockId(block);
            var totalRips = block.Body.MinerVOut.Amount;

            while (!IsNounceGood(target,block))
            {
                nounce = GenerateRandomString();
                block.TimeOfCreation = DateTime.UtcNow;
                block.Body.Nounce = nounce;
                block.TimeOfCreation = DateTime.UtcNow;
                Block.SetUniqueBlockId(block);
                tries++;
                if (tries % 100000 == 0)
                {
                    Console.WriteLine("[MinerInfo]{0}|Hashing at - {1}/Hs | Working on {2} rips of block",Thread.CurrentThread.ManagedThreadId, 100000f* 1/(DateTime.UtcNow-lastTime).TotalSeconds, totalRips.ToString());
                    lastTime = DateTime.UtcNow;
                }

                if (Terminators[id])
                {
                    Console.WriteLine("[MinerInfo]{0}|Aborting",id);
                    break;
                }
            }
            if (!Terminators[id])
            {
                Console.WriteLine("[MinerInfo]Miner finished");

                _eventManager.PassNewTrigger(block, EventTypes.EventType.NewBlock);
            }
        }

        [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
        private void RestartMiners()
        {
            var threadIds = new List<int>();
            threadIds.AddRange(Terminators.Keys.ToList());
            foreach (var threadId in threadIds)
            {
                Terminators[threadId] = true;
            }

            CurrentWorkers = new List<Thread>();
            for (int ii = 0; ii < Configurations.Config.MaxThreadsToMineOn; ii++)
            {
                var th = new Thread(new ThreadStart(Miner));
                th.Start();
                Thread.Sleep(30);
            }
        }

        private void MemPool_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var ss = JsonConvert.SerializeObject(MemPool);
            File.WriteAllText(_memPoolFullPath,ss);
            
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



