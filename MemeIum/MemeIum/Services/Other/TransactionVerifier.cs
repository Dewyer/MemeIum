using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Mineing;
using Newtonsoft.Json;

namespace MemeIum.Services.Other
{
    class TransactionVerifier : ITransactionVerifier,IService
    {
        private ILogger _logger;

        private string _unspentDbFullPath;
        private SQLiteConnection _unspentConnection;
        private IBlockChainService _blockChainService;
        private IMinerService _minerService;
        private IMappingService _mappingService;

        public void Init()
        {
            _logger = Services.GetService<ILogger>();
            _unspentDbFullPath = $"{Configurations.CurrentPath}/BlockChain/Data/Unspent.sqlite";

            var ev = Services.GetService<IEventManager>();
            ev.RegisterEventListener(OnNewBlock,EventTypes.EventType.NewVerifiedBlock);
            ev.RegisterEventListener(OnNewTransaction, EventTypes.EventType.NewTransaction);

            _blockChainService = Services.GetService<IBlockChainService>();
            _minerService = Services.GetService<IMinerService>();
            _mappingService = Services.GetService<IMappingService>();

            TryConnectToUnspentDB();

            if (!IsLoadedIn(_blockChainService.Info.EndOfLongestChain))
            {
                _logger.Log("Need to backload the Unspent db for some reasons");
                LoadEveryNewBlock();
            }
        }

        public void CreateBaseDb()
        {
            string sql = @"CREATE TABLE unspent (id varchar(50) PRIMARY KEY,fromaddr varchar(50),toaddr varchar(50),amount varchar(70),inblock varchar(50),spent varchar(1));";
            sql += "CREATE TABLE loadeds (id varchar(50));";

            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = sql;

            cmd.ExecuteNonQuery();
        }

        public void LoadEveryNewBlock()
        {
            var chainPath = $"{Configurations.CurrentPath}/BlockChain/Chain/";
            var blocks = Directory.GetFiles(chainPath);

            foreach (var block in blocks)
            {
                var pathTokens = block.Split('/');
                var id = pathTokens[pathTokens.Length - 1].Split('.')[0];
                var bb = _blockChainService.LookUpBlock(id);

                if (!IsLoadedIn(bb.Body.Id))
                {
                    _logger.Log($"Backloaded :{bb.Body.Id}-block");
                    LoadBlockToUnspentDb(bb);
                }
            }
        }

        public bool IsLoadedIn(string id)
        {
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = "SELECT * FROM loadeds WHERE id=$id";
            cmd.Parameters.AddWithValue("id", id);
            var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                return true;
            }
            return false;

        }

        public void SetAsLoaded(string toId)
        {
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = "INSERT INTO loadeds (id) VALUES ($id)";
            cmd.Parameters.AddWithValue("id", toId);
            cmd.ExecuteNonQuery();
        }

        public List<TransactionVOut> GetAllTransactionVOutsForAddress(string addr)
        {
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = "SELECT * FROM unspent WHERE toaddr=$to AND spent='0';";
            cmd.Parameters.AddWithValue("to", addr);
            var reader = cmd.ExecuteReader();
            var ts = new List<TransactionVOut>();
            while (reader.Read())
            {
                var trans = TransactionVOut.GetVoutFromSqlReader(reader);
                if (_blockChainService.IsBlockInLongestChain(trans.FromBlock))
                    ts.Add(trans);
            }

            return ts;
        }

        public void SpendInput(string id)
        {
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = "UPDATE unspent SET spent='1' WHERE id=$id";
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();

        }

        public void LoadBlockToUnspentDb(Block block)
        {
            foreach (var transaction in block.Body.Tx)
            {
                foreach (var vout in transaction.Body.VOuts)
                {
                    RegisterVout(vout, block);
                }
                foreach (var input in transaction.Body.VInputs)
                {
                    SpendInput(input.OutputId);
                }
            }
            RegisterVout(block.Body.MinerVOut,block);
            SetAsLoaded(block.Body.Id);
        }

        public void OnNewTransaction(object obj)
        {
            var trans = (Transaction)obj;
            if (Verify(trans))
            {
                _logger.Log($"New transaction {trans.Body.TransactionId}");
                if (!_minerService.HasTransactionInMemPool(trans.Body.TransactionId))
                {
                    _minerService.MemPool.Add(trans);
                    _minerService.TryRestartingWorkers();
                    _mappingService.Broadcast(trans);
                }
                else
                {
                    _logger.Log("Already had transaction received", 1);
                }

            }
            else {
                _logger.Log($"Bad new transaction {trans.Body.TransactionId}");
            }
        }

        public void OnNewBlock(object obj)
        {
            var block = (Block) obj;
            LoadBlockToUnspentDb(block);
        }

        public void RegisterVout(InBlockTransactionVOut voutIn,Block block)
        {
            var vout = new TransactionVOut()
            {
                Amount = voutIn.Amount,
                FromAddress = voutIn.FromAddress,
                FromBlock = block.Body.Id,
                Id=voutIn.Id,
                ToAddress = voutIn.ToAddress
            };
            var ss = GetUnspeTransactionVOut(vout.Id,out bool spent);
            if (ss != null)
            {
                return;
            }

            var cmd = vout.CreateInsertCommand();
            cmd.Connection = _unspentConnection;
            cmd.ExecuteNonQuery();
        }

        public void TryConnectToUnspentDB()
        {
            if (File.Exists(_unspentDbFullPath))
            {
                try
                {
                    _unspentConnection = new SQLiteConnection($"Data Source={_unspentDbFullPath}");
                    _unspentConnection.Open();
                }
                catch (Exception e)
                {
                    _logger.Log(e.Message, 2);
                }
            }
            else
            {
                SQLiteConnection.CreateFile(_unspentDbFullPath);
                _unspentConnection = new SQLiteConnection($"Data Source={_unspentDbFullPath}");
                _unspentConnection.Open();
                CreateBaseDb();
            }
        }

        public TransactionVOut GetUnspeTransactionVOut(string id,out bool spent)
        {
            var sql = "SELECT * FROM unspent WHERE id=$id";
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("id", id);
            var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                spent = true;
                return null;
            }

            reader.Read();
            spent = reader["spent"].ToString().Equals("1");
            return TransactionVOut.GetVoutFromSqlReader(reader);
        }

        public bool Verify(Transaction transaction)
        {
            var ss = JsonConvert.SerializeObject(transaction);

            if (ss.Length > Configurations.MAX_TRANSACTION_SIZE_BYTES)
            {
                return false;
            }

            if (!LegalityCheck(transaction))
            {
                return false;
            }

            if (!VerifySignature(transaction))
            {
                return false;
            }

            if (!VerifyAddress(transaction))
            {
                return false;
            }

            foreach (var vin in transaction.Body.VInputs)
            {
                TransactionVOut vout;
                if (!VerifyVInAsUnspent(vin, transaction, out vout))
                {
                    return false;
                }

                if (ViolatesMemPool(transaction,vin))
                {
                    return false;
                }

                var cc = transaction.Body.VInputs.FindAll(r => r.OutputId == vin.OutputId).Count;
                if (cc > 1)
                {
                    return false;
                }
            }

            if (!VerifySum(transaction))
            {
                return false;
            }

            return true;
        }

        public bool ViolatesMemPool(Transaction trans,TransactionVIn vin)
        {
            var exists = _minerService.MemPool.ToList()
                .TrueForAll(r => r.Body.VInputs.TrueForAll(x => x.OutputId != vin.OutputId) || r.Body.TransactionId == trans.Body.TransactionId);

            return !exists;
        }

        public bool LegalityCheck(Transaction transaction)
        {
            if (transaction.Body != null)
            {
                if (transaction.Body.VInputs == null || transaction.Body.VOuts == null)
                {
                    return false;
                }

                if (transaction.Body.VOuts.Count == 0 || transaction.Body.VInputs.Count == 0)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            foreach (var transactionVIn in transaction.Body.VInputs)
            {
                if (!transactionVIn.IsLegal())
                {
                    return false;
                }
            }

            foreach (var vout in transaction.Body.VOuts)
            {
                if (!vout.IsLegal())
                {
                    return false;
                }
            }

            var others = new List<string>(){transaction.Body.TransactionId, transaction.Body.FromAddress};

            foreach (var other in others)
            {
                if (string.IsNullOrEmpty(other) || other.Length>=Configurations.MAX_STR_LEN)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(transaction.Body.PubKey))
                return false;

            if (transaction.Body.VInputs.Count == 0 || transaction.Body.VOuts.Count == 0)
            {
                return false;
            }

            return true;
        }

        public bool VerifySum(Transaction transaction)
        {
            var inp = transaction.Body.VInputs.Sum(r => GetUnspeTransactionVOut(r.OutputId,out bool spent).Amount);
            var outp = transaction.Body.VOuts.Sum(r => r.Amount);
            return inp>=outp;

        }

        public bool VerifyAddress(Transaction transaction)
        {
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(transaction.Body.PubKey));
            var hashString = Convert.ToBase64String(hash);
            return hashString == transaction.Body.FromAddress;
        }

        public bool VerifyVInAsUnspent(TransactionVIn vin, Transaction transaction,out TransactionVOut vout)
        {

            bool spent;

            vout = GetUnspeTransactionVOut(vin.OutputId,out spent);
            if (vout == null)
            {
                return false;
            }
            else if (spent)
            {
                return false;
            }

            if (vin.FromBlockId != vout.FromBlock)
            {
                return false;
            }

            if (!_blockChainService.IsBlockInLongestChain(vout.FromBlock))
            {
                return false;
            }

            return true;
        }

        public bool VerifyId(Transaction transaction)
        {
            var oldH = transaction.Body.TransactionId;
            TransactionBody.SetUniqueIdForBody(transaction.Body);
            return transaction.Body.TransactionId == oldH;
        }

        public bool VerifySignature(Transaction transaction)
        {
            var pTokens = transaction.Body.PubKey.Split(' ');
            var pubKey = new RSAParameters();
            pubKey.Exponent = Convert.FromBase64String(pTokens[0]);
            pubKey.Modulus = Convert.FromBase64String(pTokens[1]);
            var DataToVerify = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(transaction.Body));
            var SignedData = Convert.FromBase64String(transaction.Signature);
            try
            { 
                RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider();
                RSAalg.ImportParameters(pubKey);

                return RSAalg.VerifyData(DataToVerify, new SHA256CryptoServiceProvider(), SignedData);

            }
            catch (CryptographicException e)
            {
                return false;
            }
        }
    }
}







