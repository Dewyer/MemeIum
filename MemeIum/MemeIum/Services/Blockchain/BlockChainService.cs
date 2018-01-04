using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MemeIum.Requests;
using MemeIum.Misc;
using Newtonsoft.Json;
using System.Data;
using System.Data.SQLite;

namespace MemeIum.Services.Blockchain
{
    class BlockChainService : IBlockChainService
    {
        private readonly ILogger _logger;

        private string _blockChainPath;
        private string _blockChainFullPath;

        public LocalChainInfo Info;

        public BlockChainService()
        {
            _blockChainPath = Configurations.CurrentPath + "\\BlockChain\\";
            _blockChainFullPath = $"{_blockChainPath}\\Chain\\";

            _logger = Services.GetService<ILogger>();

            TryLoadSavedInfo();
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
            Info.EditTime = DateTime.Now;
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
            _logger.Log("Calculateing new longest chain",1);
            var blockNames = Directory.GetFiles(_blockChainFullPath);
            
            var forkFrom = new Dictionary<string, string>();
            var forkLensFrom = new Dictionary<string,int>();
            foreach (var blockName in blockNames)
            {
                var bPath = blockName.Split('\\');
                var bId = bPath[bPath.Length - 1].Split('.')[0];
                var block = LookUpBlock(bId);
                if (block != null)
                {
                    forkFrom.Add(block.Body.Id, block.Body.LastBlockId);
                }
            }

            var maxChain = 0;
            var maxAt = "0";
            foreach (var fork in forkFrom)
            {
                var ll = getChainLen(forkFrom,forkLensFrom,fork.Key);
                if (maxChain < ll)
                {
                    maxChain = ll;
                    maxAt = fork.Key;
                }
            }

            Info.EndOfLongestChain = maxAt;
        }

        public Block LookUpBlock(string Id)
        {
            var fileName = $"{_blockChainFullPath}\\{Id}.block";
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

        public void SaveBlock(Block block)
        {
            var fileName = $"{_blockChainFullPath}\\{block.Body.Id}.block";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName,JsonConvert.SerializeObject(block));
            }
        }

        public void ParseInvitationRequest(InvitationRequest request)
        {

        }
    }
}
