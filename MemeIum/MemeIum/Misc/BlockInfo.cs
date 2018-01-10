using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Numerics;
using System.Text;

namespace MemeIum.Misc
{
    class BlockInfo
    {
        public string Id { get; set; }
        public string LastBlockId { get; set; }
        public DateTime CreationTime { get; set; }
        public int Height { get; set; }
        public string Target { get; set; }

        public static BlockInfo FromSqlReader(SQLiteDataReader reader)
        {
            var lTicks = long.Parse(reader["createdatticks"].ToString());
            var height = int.Parse(reader["height"].ToString());
            var info = new BlockInfo()
            {
                Id = reader["id"].ToString(),
                CreationTime = new DateTime().AddTicks(lTicks),
                LastBlockId = reader["lastblockid"].ToString(),
                Height = height,
                Target = reader["target"].ToString()
            };

            return info;
        }

        public static BlockInfo FromBlock(Block block)
        {
            var info = new BlockInfo()
            {
                CreationTime = block.TimeOfCreation,
                Height = block.Body.Height,
                Id = block.Body.Id,
                LastBlockId = block.Body.LastBlockId,
                Target = block.Body.Target
            };
            return info;
        }

        public SQLiteCommand GetInsertCommand()
        {
            var cmd = new SQLiteCommand();
            cmd.CommandText = "INSERT INTO blockinfo (id,lastblockid,createdatticks,height,target) VALUES ($id,$lb,$ct,$h,$t);";
            cmd.Parameters.AddWithValue("id", this.Id);
            cmd.Parameters.AddWithValue("lb", this.LastBlockId);
            cmd.Parameters.AddWithValue("ct", this.CreationTime.Ticks.ToString());
            cmd.Parameters.AddWithValue("h", this.Height.ToString());
            cmd.Parameters.AddWithValue("t", this.Target);

            return cmd;
        }
    }
}
