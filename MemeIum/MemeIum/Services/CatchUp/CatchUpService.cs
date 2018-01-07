using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Mineing;
using Newtonsoft.Json;

namespace MemeIum.Services.CatchUp
{
    class CatchUpService : ICatchUpService, IService
    {
        private ILogger _logger;
        private IBlockChainService _blockChainService;
        private IMappingService _mappingService;
        private IP2PServer _server;
        private IMinerService _minerService;

        private string _catchDataFullPath;

        public bool CaughtUp { get; private set; }
        public Thread Checker { get; set; }
        private List<Transaction> bufferedTransactions;
        private bool shouldTryEnd;
        private string supposedLongestBlockId;

        public void Init()
        {
            _logger = Services.GetService<ILogger>();
            _mappingService = Services.GetService<IMappingService>();
            _server = Services.GetService<IP2PServer>();
            _minerService = Services.GetService<IMinerService>();

            _catchDataFullPath = $"{Configurations.CurrentPath}\\BlockChain\\Catchup";
            _logger.Log("Ketchup started to flow ....",1);
            CaughtUp = false;
            bufferedTransactions = new List<Transaction>();
            supposedLongestBlockId = "";
            shouldTryEnd = false;

            if (!Directory.Exists(_catchDataFullPath))
            {
                Directory.CreateDirectory(_catchDataFullPath);
            }

            if (_mappingService.Peers.Count == 0)
            {
                CaughtUp = true;
                _logger.Log("Found no peers, ketchup.");
            }

            Checker = new Thread(new ThreadStart(CheckUpChecker));
        }

        private bool IsBlockBuffered(string id)
        {
            return File.Exists($"{_catchDataFullPath}\\{id}.json");
        }

        public void ParseCatcherUpRequest(CatcherUpRequest request,Peer from)
        {
            supposedLongestBlockId = request.EndOfLongestChain;

            foreach (var inv in request.Invs)
            {
                if (inv.IsBlock)
                {
                    if (!IsBlockBuffered(inv.DataId))
                    {
                        var req = new InvitationResponseRequest()
                        {
                            IsBlock = true,
                            WantedDataId = inv.DataId
                        };
                        _server.SendResponse(req, from);
                    }
                }
                else
                {
                    if (!bufferedTransactions.Exists(r => r.Body.TransactionId == inv.DataId))
                    {
                        var req = new InvitationResponseRequest()
                        {
                            IsBlock = false,
                            WantedDataId = inv.DataId
                        };
                        _server.SendResponse(req, from);
                    }
                }
            }
            
        }

        public Block LoadBufferedBlock(string id)
        {
            var file = $"{_catchDataFullPath}\\{id}.json";
            if (File.Exists(file))
            {
                var ss = File.ReadAllText(file);
                return JsonConvert.DeserializeObject<Block>(ss);
            }
            return null;
        }

        public bool DoesItReachDown(string id)
        {
            var block = LoadBufferedBlock(id);

            while (block.Body.Id != Configurations.GENESIS_BLOCK_ID)
            {
                block = LoadBufferedBlock(block.Body.LastBlockId);
                if (block == null)
                {
                    return false;
                }
            }
            return true;
        }

        public void ParseCatchUpData(object data)
        {
            if (data.GetType() == typeof(TransactionRequest))
            {
                var trans = (TransactionRequest)data;

                if (!bufferedTransactions.Exists(r => r.Body.TransactionId == trans.Transaction.Body.TransactionId))
                {
                    bufferedTransactions.Add(trans.Transaction);
                }

            }
            else if (data.GetType() == typeof(BlockRequest))
            {
                var block = (BlockRequest)data;

                if (!IsBlockBuffered(block.Block.Body.Id))
                {
                    File.WriteAllText($"{_catchDataFullPath}\\{block.Block.Body.Id}.json",JsonConvert.SerializeObject(block.Block));

                    if (block.Block.Body.Id == supposedLongestBlockId)
                    {
                        shouldTryEnd = true;
                    }
                }
            }

            if (shouldTryEnd)
            {
                if (DoesItReachDown(supposedLongestBlockId))
                {
                    CaughtUp = true;
                    LoadDataInOrder();
                }
            }
        }



        private void LoadDataInOrder()
        {
            _logger.Log("Caught up ! Starting ..");
            var files = Directory.GetFiles(_catchDataFullPath);
            foreach (var file in files)
            {
                var tokens = file.Split('\\');
                var name = tokens[tokens.Length - 1];
                _logger.Log("Copied new block from catch : "+name);
                File.Copy(file,$"{Configurations.CurrentPath}\\BlockChain\\Chain\\{name}");
                
            }
            _blockChainService.TryLoadSavedInfo();
            _minerService.TryRestartingWorkers();
        }

        public void CheckUpChecker()
        {
            var rng = new Random();
            while (!CaughtUp)
            {
                var req = new DidICatchUpRequest()
                {
                    LastKnownEnd = _blockChainService.Info.EndOfLongestChain,
                    LastOnline =  _blockChainService.Info.EditTime.AddMilliseconds(1)
                };
                _mappingService.Peers.Shuffle();
                for (int ii = 0; ii < Configurations.CATCHUP_N;ii++)
                {
                    _server.SendResponse(req,_mappingService.Peers[ii]);
                }

                Thread.Sleep(1000*Configurations.Config.SecondsToWaitBetweenCatchUpLoops);
            }
        }
    }
}
