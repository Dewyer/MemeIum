using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
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
        private IEventManager _eventManager;

        private string _catchDataFullPath;

        public bool CaughtUp { get; private set; }
        public Thread Checker { get; set; }
        private List<Transaction> bufferedTransactions;
        private List<BlockInfo> bufferedBlockInfos;
        private bool shouldTryEnd;
        private string supposedLongestBlockId;
        private DateTime LastGotResponse;

        public void Init()
        {
            _logger = Services.GetService<ILogger>();
            _mappingService = Services.GetService<IMappingService>();
            _server = Services.GetService<IP2PServer>();
            _minerService = Services.GetService<IMinerService>();
            _eventManager = Services.GetService<IEventManager>();
            _blockChainService = Services.GetService<IBlockChainService>();

            _catchDataFullPath = $"{Configurations.CurrentPath}\\BlockChain\\Catchup";
            _logger.Log("Ketchup started to flow ....",1);
            CaughtUp = false;
            bufferedTransactions = new List<Transaction>();
            bufferedBlockInfos = new List<BlockInfo>();
            supposedLongestBlockId = "";
            shouldTryEnd = false;
            LastGotResponse = DateTime.UtcNow;

            if (!Directory.Exists(_catchDataFullPath))
            {
                Directory.CreateDirectory(_catchDataFullPath);
            }
        }

        public void StartCatchup()
        {
            if (_mappingService.Peers.Count == 0)
            {
                CaughtUp = true;
                _logger.Log("Found no peers, ketchup.");
            }

            Checker = new Thread(new ThreadStart(CheckUpChecker));
            Checker.Start();
        }

        private bool IsBlockBuffered(string id)
        {
            return bufferedBlockInfos.FindAll(r=>r.Id == id).Count > 0;
        }

        public void ParseCatcherUpRequest(CatcherUpRequest request,Peer from)
        {
            supposedLongestBlockId = request.EndOfLongestChain;
            _logger.Log($"Ketchupper {request.EndOfLongestChain} - Count:{request.Invs.Count}");

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

            if (request.EndOfLongestChain == _blockChainService.Info.EndOfLongestChain && request.Invs.Count == 0)
            {
                CaughtUp = true;
                _logger.Log("Ketchup cas its ended and no invs.");
            }
        }

        public Block LoadBufferedBlock(string id)
        {
            var newId = id.Replace('/', '-');
            var file = $"{_catchDataFullPath}\\{newId}.json";
            if (File.Exists(file))
            {
                var ss = File.ReadAllText(file);
                return JsonConvert.DeserializeObject<Block>(ss);
            }
            return null;
        }

        public bool IsBlockChainReachDown(string id)
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
            LastGotResponse = DateTime.UtcNow;
            
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
                    var newId = block.Block.Body.Id.Replace('/', '-');
                    File.WriteAllText($"{_catchDataFullPath}\\{newId}.json",JsonConvert.SerializeObject(block.Block));

                    if (block.Block.Body.Id == supposedLongestBlockId)
                    {
                        shouldTryEnd = true;
                    }
                    var bInfo = new BlockInfo()
                    {
                        Id = block.Block.Body.Id,
                        CreationTime = block.Block.TimeOfCreation,
                        Height = block.Block.Body.Height,
                        LastBlockId = block.Block.Body.LastBlockId
                    };
                    bufferedBlockInfos.Add(bInfo);
                }
            }

            if (shouldTryEnd)
            {
                if (IsBlockChainReachDown(supposedLongestBlockId))
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
            //process blocks in order
            var order = bufferedBlockInfos.OrderBy(r => r.Height).ToList();

            foreach (var blockInfo in order)
            {
                var bb = _blockChainService.LookUpBlock(blockInfo.Id);
                _eventManager.PassNewTrigger(bb,EventTypes.EventType.NewBlock);
            }
            _blockChainService.TryLoadSavedInfo();
            _minerService.TryRestartingWorkers();
        }

        public void CheckUpChecker()
        {
            while (!CaughtUp)
            {
                var req = new DidICatchUpRequest()
                {
                    LastKnownEnd = _blockChainService.Info.EndOfLongestChain,
                    LastOnline =  _blockChainService.Info.EditTime.AddMilliseconds(1)
                };
                _mappingService.Peers.Shuffle();
                for (int ii = 0; ii < (_mappingService.Peers.Count >= Configurations.CATCHUP_N ? Configurations.CATCHUP_N : _mappingService.Peers.Count);ii++)
                {
                    _server.SendResponse(req,_mappingService.Peers[ii]);
                }

                if ((DateTime.UtcNow - LastGotResponse).TotalSeconds >= Configurations.MAX_TIME_BETWEEN_CATCHUP_RESP)
                {
                    CaughtUp = true;
                    _logger.Log("Ketchup timed out, ketchup.");
                    break;
                }

                Thread.Sleep(1000*Configurations.Config.SecondsToWaitBetweenCatchUpLoops);
            }
        }

        public void ParseDidICatchUp(DidICatchUpRequest request, Peer from)
        {
            var catcherUp = new CatcherUpRequest()
            {
                EndOfLongestChain = _blockChainService.Info.EndOfLongestChain,
                Invs = new List<InvitationRequest>()
            };

            foreach (var trans in _minerService.MemPool)
            {
                var inv = new InvitationRequest()
                {
                    DataId = trans.Body.TransactionId,
                    IsBlock = false
                };
                catcherUp.Invs.Add(inv);
            }

            if (request.LastKnownEnd != _blockChainService.Info.EndOfLongestChain)
            {
                var newBs = _blockChainService.GetNewerBlockInfos(request.LastOnline, request.LastKnownEnd);
                foreach (var blockInfo in newBs)
                {
                    var inv = new InvitationRequest()
                    {
                        DataId = blockInfo.Id,
                        IsBlock = true
                    };
                    catcherUp.Invs.Add(inv);
                }
            }
            Console.WriteLine("Response to didI {0} {1}",catcherUp.EndOfLongestChain,catcherUp.Invs.Count);
            _server.SendResponse(catcherUp,from);
        }
    }
}
