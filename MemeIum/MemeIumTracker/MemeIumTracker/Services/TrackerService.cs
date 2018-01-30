using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumTracker.Services
{
    public interface ITrackerService
    {
        List<Peer> GetAllPeers();
        bool SignInPeer(string ip,int port);
        void Reset();
    }

    public class Peer
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public static Peer FromReader(SqlDataReader reader,out DateTime lastUp)
        {
            var ipParts = reader["ip"].ToString().Split(':');
            var port = 0;

            if (!int.TryParse(ipParts[1].ToString(), out port))
            {
                lastUp = new DateTime(0);
                return null;
            }
            var pp = new Peer()
            {
                Address = ipParts[0],
                Port = port
            };
            lastUp = new DateTime().AddTicks(long.Parse(reader["lastrespticks"].ToString()));
            return pp;
        }

        public override string ToString()
        {
            return this.Address + "|" + this.Port;
        }
    }

    public class InMemo : ITrackerService
    {
        public List<Peer> All;
        public Dictionary<string, DateTime> Times;

        public InMemo()
        {
            All = new List<Peer>();
            Times = new Dictionary<string, DateTime>();
        }

        public List<Peer> GetAllPeers()
        {
            var stay = new List<Peer>();
            var newTimes = new Dictionary<string,DateTime>();
            foreach (var peer in All)
            {
                if ((DateTime.UtcNow - Times[peer.ToString()]).TotalSeconds <= 10)
                {
                    stay.Add(peer);
                    if (!newTimes.ContainsKey(peer.ToString()))
                        newTimes.Add(peer.ToString(),Times[peer.ToString()]);
                }
            }
            All = stay;
            Times = newTimes;
            return All;
        }

        public bool SignInPeer(string ip,int port)
        {
            var peer = new Peer()
            {
                Address = ip,
                Port = port
            };
            if (Times.ContainsKey(peer.ToString()))
            {
                Times[peer.ToString()] = DateTime.UtcNow;
            }
            else
            {
                All.Add(peer);
                Times.Add(peer.ToString(),DateTime.UtcNow);
            }
            return true;
        }

        public void Reset()
        {
            All = new List<Peer>();
            Times = new Dictionary<string, DateTime>();
        }
    }

    public class TrackerService : ITrackerService
    {
        private string _dbPath;
        private SqlConnection connection;

        public TrackerService()
        {
            TryConnectToDb();
        }

        private void TryConnectToDb()
        {
            connection = new SqlConnection("Server=tcp:voidloop.database.windows.net,1433;Initial Catalog=tracker;Persist Security Info=False;User ID=rooter;Password=FuckMePls!0;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE track (ip varchar(100),lastrespticks INTEGER);";
            cmd.ExecuteNonQuery();
        }

        public List<Peer> GetAllPeers()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText= "SELECT * FROM track ORDER BY lastrespticks";
            var reader = cmd.ExecuteReader();

            var peers = new List<Peer>();
            while (reader.Read())
            {
                var pp = Peer.FromReader(reader,out DateTime last);

                if ((DateTime.UtcNow - last).TotalMinutes <= 20)
                {
                    peers.Add(pp);
                }
                else
                {
                    DeletePeer(pp);
                }
            }
            return peers;
        }

        public void DeletePeer(Peer peer)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM track WHERE ip=$ip;";
            cmd.Parameters.AddWithValue("ip", peer.Address + ":" + peer.Port.ToString());

            cmd.ExecuteNonQuery();
        }

        public void CreatePeer(Peer peer)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO track (ip,lastrespticks) VALUES ($ip,$last);";
            cmd.Parameters.AddWithValue("ip", peer.Address + ":" + peer.Port.ToString());
            cmd.Parameters.AddWithValue("last", DateTime.UtcNow.Ticks.ToString());

            cmd.ExecuteNonQuery();

        }

        public bool SignInPeer(string ip,int port)
        {
            try
            {
                var peer = new Peer()
                {
                    Address = ip.Split(':')[0],
                    Port = int.Parse(ip.Split(':')[1])
                };
                DeletePeer(peer);
                CreatePeer(peer);
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}

