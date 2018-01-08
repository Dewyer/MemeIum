using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MemeIum.Requests;
using MemeIum.Misc;
using Newtonsoft.Json;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using MemeIum.Misc.Transaction;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Mineing;
using MemeIum.Services.Other;

namespace MemeIum.Services.Blockchain
{
    class BlockChainService : IBlockChainService, IService
    {
        private ILogger _logger;
        private ITransactionVerifier _transactionVerifier;
        private IBlockVerifier _blockVerifier;
        private IP2PServer _server;
        private IEventManager _eventManager;
        private IMappingService _mappingService;
        private IMinerService _minerService;

        private string _blockChainPath;
        private string _blockChainFullPath;

        private List<InvitationRequest> _askedForRequests;

        public LocalChainInfo Info { get; set; }

        public void Init()
        {
            _blockChainPath = Configurations.CurrentPath + "\\BlockChain\\";
            _blockChainFullPath = $"{_blockChainPath}\\Chain\\";

            _logger = Services.GetService<ILogger>();
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _blockVerifier = Services.GetService<IBlockVerifier>();
            _server = Services.GetService<IP2PServer>();
            _eventManager = Services.GetService<IEventManager>();
            _eventManager.RegisterEventListener(OnNewBlock,EventTypes.EventType.NewBlock);
            _mappingService = Services.GetService<IMappingService>();
            _minerService = Services.GetService<IMinerService>();

            TryLoadSavedInfo();
            
            _askedForRequests = new List<InvitationRequest>();
        }

        private void OnNewBlock(object obj)
        {
            var block = (Block) obj;
            if (_blockVerifier.Verify(block))
            {
                SaveBlock(block);

                if (block.Body.LastBlockId == Info.EndOfLongestChain)
                {
                    Info.EndOfLongestChain = block.Body.Id;
                    Info.Height++;
                }
                else
                {
                    CalculateLongestChain();
                }
                Info.EditTime = DateTime.UtcNow;
                SaveLocalInfo();
                _eventManager.PassNewTrigger(block,EventTypes.EventType.NewVerifiedBlock);
                BroadCastVerifiedBlock(block);
            }
        }

        private void BroadCastVerifiedBlock(Block block)
        {
            var req = new InvitationRequest()
            {
                DataId = block.Body.Id,
                IsBlock = true
            };
            _mappingService.Broadcast(req);
        }

        public void TryLoadSavedInfo()
        {
            var infoPath = $"{_blockChainPath}\\Data\\info.json";
            Info = null;
            if (File.Exists(infoPath))
            {
                try
                {
                    Info = JsonConvert.DeserializeObject<LocalChainInfo>(File.ReadAllText(infoPath));
                }
                catch (Exception e)
                {
                    Info = null;
                }
            }

            if (Info == null)
            {
                _logger.Log("Could not load local BC info!",1);
                CreateNewLocalInfo();
            }

            var dirTime = Directory.GetLastWriteTimeUtc(_blockChainFullPath);
            if (dirTime > Info.EditTime)
            {
                _logger.Log("Deleteing old info because an edit has been done",1);
                CreateNewLocalInfo();
            }
        }

        public void SaveLocalInfo()
        {
            var infoPath = $"{_blockChainPath}\\Data\\info.json";
            File.WriteAllText(infoPath,JsonConvert.SerializeObject(Info));
        }

        public void CreateNewLocalInfo()
        {
            Info = new LocalChainInfo();
            CalculateLongestChain();
            Info.EditTime = DateTime.UtcNow;
            SaveLocalInfo();
        }

        private int getChainLen(Dictionary<string, string> tree,Dictionary<string,int> forkLens,string id)
        {
            var ll = tree[id];
            var len = 0;
            while (ll != "0")
            {
                ll = tree[ll];
                len++;
                if (forkLens.ContainsKey(ll))
                {
                    len = forkLens[ll];
                    break;
                }
                else
                {
                    forkLens.Add(ll, len);
                }
            }
            len++;
            return len;
        }

        public void CalculateLongestChain()
        {
            _logger.Log("Calculateing new longest chain", 1);
            var blockNames = Directory.GetFiles(_blockChainFullPath);

            var forkFrom = new Dictionary<string, string>();
            var forkLensFrom = new Dictionary<string, int>();
            foreach (var blockName in blockNames)
            {
                var bPath = blockName.Split('\\');
                var bId = bPath[bPath.Length - 1].Split('.')[0];
                var block = LookUpBlockInfo(bId);
                if (block != null)
                {
                    forkFrom.Add(block.Id, block.LastBlockId);
                }
            }

            var maxChain = 0;
            var maxAt = "0";
            foreach (var fork in forkFrom)
            {
                var ll = getChainLen(forkFrom, forkLensFrom, fork.Key);
                if (maxChain < ll)
                {
                    maxChain = ll;
                    maxAt = fork.Key;
                }
            }

            Info.EndOfLongestChain = maxAt;
            Info.Height = maxChain;
        }

        public Block LookUpBlock(string Id)
        {
            var newId = Id.Replace('/', '-');
            var fileName = $"{_blockChainFullPath}\\{newId}.block";
            if (File.Exists(fileName))
            {
                var text = File.ReadAllText(fileName);
                try
                {
                    return JsonConvert.DeserializeObject<Block>(text);
                }
                catch (Exception ex)
                {
                    _logger.Log("Could not parse block!",2);
                }
            }
            return null;
        }

        public BlockInfo LookUpBlockInfo(string Id)
        {
            var bb = LookUpBlock(Id);
            var info = new BlockInfo()
            {
                Id=Id,
                CreationTime = bb.TimeOfCreation,
                LastBlockId = bb.Body.LastBlockId,
                Height = bb.Body.Height
            };

            return info;
        }

        public BlockInfo LookUpBlockInfoByHeight(int height)
        {
            var atB = LookUpBlockInfo(Info.EndOfLongestChain);
            for (int ii = 0; ii < Info.Height - height; ii++)
            {
                atB = LookUpBlockInfo(atB.LastBlockId);
            }
            return atB;
        }

        public bool IsBlockInLongestChain(string blockid)
        {
            var at = Info.EndOfLongestChain;

            while (at != blockid)
            {
                at = LookUpBlockInfo(at).LastBlockId;

                if (at == Configurations.GENESIS_BLOCK_ID)
                {
                    return false;
                }
            }
            return true;
        }

        public void SaveBlock(Block block)
        {
            var newId = block.Body.Id.Replace('/', '-');
            var fileName = $"{_blockChainFullPath}\\{newId}.block";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName,JsonConvert.SerializeObject(block));
            }
        }

        public bool WantedTransaction(InvitationRequest transInvitationRequest)
        {
            var atB = LookUpBlock(Info.EndOfLongestChain);
            for (int ii = 0; ii < Configurations.TRANSACTION_WANT_LIMIT; ii++)
            {
                foreach (var transaction in atB.Body.Tx)
                {
                    if (transaction.Body.TransactionId == transInvitationRequest.DataId)
                    {
                        return false;
                    }
                }
                atB = LookUpBlock(atB.Body.LastBlockId);
            }
            return true;
        }

        public void ParseInvitationRequest(InvitationRequest request,Peer from)
        {
            var req = new InvitationResponseRequest()
            {
                IsBlock = request.IsBlock,
                WantedDataId = request.DataId
            };
            var took = false;

            if (_askedForRequests.Exists(r => r.DataId == request.DataId && r.IsBlock == request.IsBlock))
            {
                return;
            }

            if (request.IsBlock)
            {
                var bb = LookUpBlock(request.DataId);

                if (bb == null)
                {
                    took = true;
                }
            }
            else
            {
                if (WantedTransaction(request))
                {
                    took = true;
                }
            }

            if (took)
            {
                _askedForRequests.Add(request);
                _server.SendResponse(req, from);
            }
        }

        public Transaction TryGetTransaction(string id)
        {
            var txs = _minerService.MemPool.ToList().FindAll(r => r.Body.TransactionId == id);
            if (txs.Count == 1)
            {
                return txs[0];
            }
            return null;
        }

        public void ParseInvitationResponseRequest(InvitationResponseRequest request, Peer from)
        {
            if (request.IsBlock)
            {
                var bb = LookUpBlock(request.WantedDataId);

                if (bb != null)
                {
                    var req = new BlockRequest()
                    {
                        Block = bb
                    };
                    _server.SendResponse(req, from);
                }
            }
            else
            {
                var tt = TryGetTransaction(request.WantedDataId);
                if (tt != null)
                {
                    var req = new TransactionRequest()
                    {
                        Transaction = tt
                    };

                    _server.SendResponse(req, from);
                }
            }
        }

        public void ParseDataRequest(object data)
        {
            if (data.GetType() == typeof(TransactionRequest))
            {
                var trans = (TransactionRequest) data;
                if (TryGetTransaction(trans.Transaction.Body.TransactionId) == null)
                {
                    if (_transactionVerifier.Verify(trans.Transaction))
                    {
                        _eventManager.PassNewTrigger(trans.Transaction, EventTypes.EventType.NewTransaction);
                    }
                }
            }
            else if (data.GetType() == typeof(BlockRequest))
            {
                var block = (BlockRequest)data;
                _eventManager.PassNewTrigger(block.Block,EventTypes.EventType.NewBlock);
            }
        }

        public List<BlockInfo> GetNewerBlockInfos(DateTime fromtime, string tillId)
        {
            var bb = new List<BlockInfo>();
            var atB = LookUpBlockInfo(Info.EndOfLongestChain);
            while (atB.CreationTime >= fromtime)
            {
                bb.Add(atB);
                atB = LookUpBlockInfo(atB.LastBlockId);
            }

            return bb;
        }
    }
}
