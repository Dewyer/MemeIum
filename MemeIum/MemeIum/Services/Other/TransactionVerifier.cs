using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MemeIum.Misc;
using MemeIum.Misc.Transaction;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;
using Newtonsoft.Json;

namespace MemeIum.Services.Other
{
    class TransactionVerifier : ITransactionVerifier
    {
        private readonly ILogger _logger;

        private string _unspentDbFullPath;
        private SQLiteConnection _unspentConnection;
        private readonly IBlockChainService _blockChainService;

        public TransactionVerifier()
        {
            _logger = Services.GetService<ILogger>();
            _unspentDbFullPath = $"{Configurations.CurrentPath}\\BlockChain\\Data\\Unspent.sqlite";

            var ev = Services.GetService<IEventManager>();
            ev.RegisterEventListener(OnNewBlock,EventTypes.EventType.NewVerifiedBlock);

            _blockChainService = Services.GetService<IBlockChainService>();

            TryConnectToUnspentDB();
        }

        public void CreateBaseDb()
        {
            string sql = @"CREATE TABLE unspent (id varchar(50) PRIMARY KEY,fromaddr varchar(50),toaddr varchar(50),amount varchar(70),inblock varchar(50));";
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = sql;

            cmd.ExecuteNonQuery();
        }

        public void OnNewBlock(object obj)
        {
            var block = (Block) obj;
            foreach (var transaction in block.Body.Tx)
            {
                foreach (var vout in transaction.Body.VOuts)
                {
                    RegisterVout(vout);
                }
            }

        }

        public void RegisterVout(TransactionVOut vout)
        {
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

        public TransactionVOut GetUnspeTransactionVOut(string id)
        {
            var sql = "SELECT * FROM unspent WHERE id=$id";
            var cmd = _unspentConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("id", id);
            var reader = cmd.ExecuteReader();
            if (reader.FieldCount != 1)
            {
                return null;
            }

            reader.Read();
            return TransactionVOut.GetVoutFromSqlReader(reader);
        }

        public bool Verify(Transaction transaction)
        {
            var ss = JsonConvert.SerializeObject(transaction);

            if (ss.Length > Configurations.MAX_TRANSACTION_SIZE_BYTES)
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

            if (!VerifyId(transaction))
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
            }

            return true;
        }

        public bool VerifyAddress(Transaction transaction)
        {
            var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(transaction.Body.PubKey));
            var hashString = Convert.ToBase64String(hash);
            return hashString == transaction.Body.FromAddress;
        }

        public bool VerifyVInAsUnspent(TransactionVIn vin, Transaction transaction,out TransactionVOut vout)
        {

            vout = GetUnspeTransactionVOut(vin.OutputId);
            if (vout == null)
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
            var id = transaction.Body.TransactionId;
            transaction.Body.TransactionId = "42";
            var ss = JsonConvert.SerializeObject(transaction.Body);
            var hash = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ss)));
            transaction.Body.TransactionId = id;
            return hash == id;
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

                return RSAalg.VerifyData(DataToVerify, new SHA1CryptoServiceProvider(), SignedData);

            }
            catch (CryptographicException e)
            {
                return false;
            }
        }
    }
}







