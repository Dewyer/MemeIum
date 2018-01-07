using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MemeIum.Misc
{
    class TransactionVOut
    {
        public string FromBlock { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public int Amount { get; set; }
        public string Id { get; set; }

        public static void SetUniqueIdForVOut(TransactionVOut vout)
        {
            vout.Id = "42";
            var ss = JsonConvert.SerializeObject(vout)+DateTime.UtcNow.Ticks.ToString();
            var hash = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ss)));
            vout.Id = hash;
        }

        public static TransactionVOut GetVoutFromSqlReader(SQLiteDataReader reader)
        {
            int valueInt;
            if (!int.TryParse(reader["amount"].ToString(), out valueInt))
            {
                return null;
            }

            var vout = new TransactionVOut()
            {
                Id = reader["id"].ToString(),
                FromAddress = reader["fromaddr"].ToString(),
                ToAddress = reader["toaddr"].ToString(),
                Amount = valueInt,
                FromBlock = reader["inblock"].ToString()
            };
            return vout;
        }

        public SQLiteCommand CreateInsertCommand()
        {
            var cmd = new SQLiteCommand();
            cmd.CommandText = "INSERT INTO unspent (id,fromaddr,toaddr,amount,inblock,spent) VALUES ($id,$from,$to,$am,$block,'0');";
            cmd.Parameters.AddWithValue("id", this.Id);
            cmd.Parameters.AddWithValue("from",this.FromAddress);
            cmd.Parameters.AddWithValue("to", this.ToAddress);
            cmd.Parameters.AddWithValue("am", this.Amount);
            cmd.Parameters.AddWithValue("block", this.FromBlock);

            return cmd;
        }
    }
}
