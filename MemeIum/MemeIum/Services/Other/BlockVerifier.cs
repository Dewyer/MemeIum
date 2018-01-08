using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Mineing;
using Newtonsoft.Json;

namespace MemeIum.Services.Other
{
    class BlockVerifier : IBlockVerifier,IService
    {
        private ITransactionVerifier _transactionVerifier;
        private IDifficultyService _difficultyService;
        private IBlockChainService _blockChainService;
        private IMinerService _minerService;

        public void Init()
        {
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _difficultyService = Services.GetService<IDifficultyService>();
            _blockChainService = Services.GetService<IBlockChainService>();
            _minerService = Services.GetService<IMinerService>();

        }

        public bool Verify(Block block)
        {
            var ss = JsonConvert.SerializeObject(block);
            if (ss.Length > Configurations.MAX_BLOCK_SIZE_BYTES)
            {
                return false;
            }

            if (!VerifyHash(block))
            {
                return false;
            }

            if (!VerifyBlockHeight(block))
            {
                return false;
            }

            if (!VerifyTarget(block))
            {
                return false;
            }

            foreach (var transaction in block.Body.Tx)
            {
                if (!_transactionVerifier.Verify(transaction))
                {
                    return false;
                }
            }

            if (!VerifyMinerVOut(block))
            {
                return false;
            }

            return true;
        }

        public bool VerifyBlockHeight(Block block)
        {
            var last = _blockChainService.LookUpBlockInfo(block.Body.LastBlockId);
            if (last !=null)
            {
                return block.Body.Height == last.Height + 1;
            }
            return false;
        }

        public bool VerifyMinerVOut(Block block)
        {
            var hh = _minerService.GetMinerVOut(block.Body.Tx, block.Body.Height);
            return block.Body.MinerVOut.Amount == hh.Amount;
        }

        public bool VerifyTarget(Block block)
        {
            var last = _blockChainService.LookUpBlockInfo(block.Body.LastBlockId);
            var info = new BlockInfo()
            {
                CreationTime = block.TimeOfCreation,
                Height = last.Height + 1,
                Id = block.Body.Id,
                LastBlockId = block.Body.LastBlockId,
                Target = ""
            };
            var target = _difficultyService.TargetForBlock(info);
            var bb = new BigInteger(Convert.FromBase64String(block.Body.Id));
            return target >= bb;
        }

        public bool VerifyHash(Block block)
        {
            var oldH = block.Body.Id;
            Block.SetUniqueBlockId(block);
            bool suc = oldH == block.Body.Id;
            block.Body.Id = oldH;
            return suc;
        }
    }
}

