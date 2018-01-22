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
using System.Threading.Tasks;
using MemeIum.Misc.Transaction;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Mineing;
using MemeIum.Services.Other;
using MemeIum.Services.Wallet;

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
        private IWalletService _walletService;

        private string _blockChainPath;
        private string _blockChainFullPath;

        private List<InvitationRequest> _askedForRequests;
        private SQLiteConnection _blockInfoDb;
        private string _blockInfoDbFullPath;

        public LocalChainInfo Info { get; set; }

        public void Init()
        {
            _blockChainPath = Configurations.CurrentPath + "\\BlockChain\\";
            _blockChainFullPath = $"{_blockChainPath}\\Chain\\";
            _blockInfoDbFullPath = _blockChainPath + "\\Data\\BlockInfo.sqlite";

            _logger = Services.GetService<ILogger>();
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _blockVerifier = Services.GetService<IBlockVerifier>();
            _server = Services.GetService<IP2PServer>();
            _eventManager = Services.GetService<IEventManager>();
            _eventManager.RegisterEventListener(OnNewBlock,EventTypes.EventType.NewBlock);
            _mappingService = Services.GetService<IMappingService>();
            _minerService = Services.GetService<IMinerService>();
            _walletService = Services.GetService<IWalletService>();

            TryConnectToBlockInfoDb();
            TryLoadSavedInfo();
            _askedForRequests = new List<InvitationRequest>();
        }

        private void CreateNewDb()
        {
            SQLiteConnection.CreateFile(_blockInfoDbFullPath);
            _blockInfoDb = new SQLiteConnection($"Data Source={_blockInfoDbFullPath}");
            _blockInfoDb.Open();
            var creatorSql = "CREATE TABLE blockinfo (id varchar(50) PRIMARY KEY,lastblockid varchar(50),createdatticks varchar(50),height INTEGER,target varchar(50));";
            var cmd = _blockInfoDb.CreateCommand();
            cmd.CommandText = creatorSql;
            cmd.ExecuteNonQuery();

            _logger.Log("Loading genesis block in.");
            SaveToDb(LookUpBlock(Configurations.GENESIS_BLOCK_ID));
        }

        private void TryConnectToBlockInfoDb()
        {
            if (!File.Exists(_blockInfoDbFullPath))
            {
                CreateNewDb();
            }
            else
            {
                _blockInfoDb = new SQLiteConnection($"Data Source={_blockInfoDbFullPath}");
                _blockInfoDb.Open();
            }
        }

        private void OnNewBlock(object obj)
        {
            var block = (Block) obj;
            if (_blockVerifier.Verify(block))
            {
                SaveBlock(block);
                _logger.Log($"Got new verified block {block.Body.Id}");
                _mappingService.Broadcast(block);

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
                CleanMemPool(block);
                _minerService.TryRestartingWorkers();
                _eventManager.PassNewTrigger(block, EventTypes.EventType.NewVerifiedBlock);
                Task.Run(delegate
                {
                    Task.Delay(2000).Wait();
                    if (_minerService.MemPool.Count == 0 &&Configurations.CAKE_MODE)
                    {
                        _minerService.MemPool.Add(_walletService.AssembleTransaction(_walletService.Address, 1, "getme"));
                        _minerService.TryRestartingWorkers();
                    }
                });
            }
            else
            {
                _logger.Log($"Got new shit block {block.Body.Id}");
            }
        }

        public void CleanMemPool(Block block)
        {            
            var tsNotOn = new List<Transaction>();
            foreach (var transaction in _minerService.MemPool)
            {
                if (block.Body.Tx.FindAll(r => r.Body.TransactionId == transaction.Body.TransactionId).Count == 0)
                {
                    tsNotOn.Add(transaction);
                }
                else
                {
                    _logger.Log($"Removed a transaction from the mempool {transaction.Body.TransactionId}");
                }
            }

            _minerService.MemPool.Clear();
            foreach (var transaction in tsNotOn)
            {
                _minerService.MemPool.Add(transaction);
            }
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
                catch
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

        public void CalculateLongestChain()
        {
            _logger.Log("Calculateing new longest chain", 1);
            var cmd = _blockInfoDb.CreateCommand();
            cmd.CommandText = "SELECT * FROM blockinfo ORDER BY height DESC;";
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                var info = BlockInfo.FromSqlReader(reader);
                Info.EndOfLongestChain = info.Id;
                Info.Height = info.Height;
            }
            else
            {
                Info.EndOfLongestChain = "0";
                Info.Height = -1;

            }
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
                catch
                {
                    _logger.Log("Could not parse block!",2);
                }
            }
            return null;
        }

        public BlockInfo LookUpBlockInfo(string id)
        {
            var cmd = _blockInfoDb.CreateCommand();
            cmd.CommandText = "SELECT * FROM blockinfo WHERE id=$id";
            cmd.Parameters.AddWithValue("id", id);
            var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return null;
            }
            reader.Read();

            return BlockInfo.FromSqlReader(reader);
        }

        public BlockInfo LookUpBlockInfoByHeight(int height)
        {
            var cmd = _blockInfoDb.CreateCommand();
            cmd.CommandText = "SELECT * FROM blockinfo WHERE height=$h";
            cmd.Parameters.AddWithValue("h", height);
            var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return null;
            }
            reader.Read();

            return BlockInfo.FromSqlReader(reader);
        }

        public bool IsBlockInLongestChain(string blockid)
        {
            var at = Info.EndOfLongestChain;

            while (at != blockid)
            {
                var b = LookUpBlockInfo(at);
                if (b == null)
                {
                    return false;
                }
                at =b.LastBlockId;
            }
            return true;
        }

        public void SaveToDb(Block block)
        {
            var last = LookUpBlockInfo(block.Body.LastBlockId);
            if (last == null)
            {
                block.Body.Height = 0;
            }
            else
            {
                block.Body.Height = last.Height + 1;
            }

            var cmd = BlockInfo.FromBlock(block).GetInsertCommand();
            cmd.Connection = _blockInfoDb;
            cmd.ExecuteNonQuery();
        }

        public void SaveBlock(Block block)
        {
            var newId = block.Body.Id.Replace('/', '-');
            var fileName = $"{_blockChainFullPath}\\{newId}.block";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName,JsonConvert.SerializeObject(block));
            }
            SaveToDb(block);
        }

        public bool WantedTransaction(string id)
        {
            return WantedTransaction(new InvitationRequest() {DataId = id, IsBlock = false});
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
                if (atB.Body.LastBlockId == "0")
                {
                    break;
                }
                atB = LookUpBlock(atB.Body.LastBlockId);
            }

            if (_minerService.HasTransactionInMemPool(transInvitationRequest.DataId))
            {
                return false;
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
                if (WantedTransaction(request) && _minerService.MemPool.ToList().FindAll(r => r.Body.TransactionId == request.DataId).Count == 0)
                {
                    took = true;
                }
            }

            if (took)
            {
                _askedForRequests.Add(request);
                _logger.Log($"Asked for data {request.DataId} ISBlock:{request.IsBlock}");
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
                _logger.Log($"Got new inv response : {request.WantedDataId}");
                if (bb != null)
                {
                    var req = new BlockRequest()
                    {
                        Block = bb
                    };
                    _logger.Log($"Sending new inv response : {request.WantedDataId}");
                    _server.SendResponse(req, from);
                }
            }
            else
            {
                var tt = TryGetTransaction(request.WantedDataId);
                _logger.Log($"Got new inv response : {request.WantedDataId}");
                if (tt != null)
                {
                    var req = new TransactionRequest()
                    {
                        Transaction = tt
                    };
                    _logger.Log($"Sending new inv response : {request.WantedDataId}");
                    _server.SendResponse(req, from);
                }
            }
        }

        public void ParseDataRequest(object data)
        {
            if (data.GetType() == typeof(TransactionRequest))
            {
                var trans = (TransactionRequest) data;
                _logger.Log($"Got new trans data : {trans.Transaction.Body.TransactionId}");
                if (TryGetTransaction(trans.Transaction.Body.TransactionId) == null)
                {
                    if (_transactionVerifier.Verify(trans.Transaction))
                    {
                        _logger.Log($"Got new nice transaction");
                        _eventManager.PassNewTrigger(trans.Transaction, EventTypes.EventType.NewTransaction);
                    }
                    else
                    {
                        _logger.Log("Got new ugly transaction.");
                    }
                }
                else
                {
                    _logger.Log("Got new had transaction.");
                }
            }
            else if (data.GetType() == typeof(BlockRequest))
            {
                var block = (BlockRequest)data;
                _logger.Log($"Got new block data : {block.Block.Body.Id}");
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
                if (atB.LastBlockId == "0")
                {
                    break;
                }
                atB = LookUpBlockInfo(atB.LastBlockId);
            }

            return bb;
        }
    }
}
