using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;

namespace MemeIum.Services.Other
{
    class DifficultyService : IDifficultyService,IService
    {
        private IBlockChainService _blockChainService;

        public void Init()
        {
            _blockChainService = Services.GetService<IBlockChainService>();
        }

        public BigInteger BigIntegerFromBase64(string ss)
        {
            var bb =new BigInteger(Convert.FromBase64String(ss));
            if (bb < 0)
            {
                bb *= -1;
            }
            return bb;
        }

        public BigInteger TargetForBlock(BlockInfo info)
        {
            var backStartFromBlock = (info.Height - (info.Height % Configurations.MA_SIZE_FOR_TARGET));
            var maSize = Configurations.MA_SIZE_FOR_TARGET;
            if (backStartFromBlock <= Configurations.MA_SIZE_FOR_TARGET)
            {
                var genesis = _blockChainService.LookUpBlockInfo(Configurations.GENESIS_BLOCK_ID);
                return BigIntegerFromBase64(genesis.Target);
            }
            var startBlock = _blockChainService.LookUpBlockInfoByHeight(backStartFromBlock);
            var lastTime = startBlock.CreationTime;
            var allSeconds = 0d;
            var avged = 0;
            for (int difS = 1; difS < maSize; difS++)
            {
                var blockInfo = _blockChainService.LookUpBlockInfoByHeight(backStartFromBlock - difS);
                var seconds = (lastTime - blockInfo.CreationTime).TotalSeconds;

                if (seconds < Configurations.MAX_SECONDS_FOR_TARGET_AVERAGE)
                {
                    allSeconds += seconds;
                    avged++;
                }

            }
            var avgSeconds = allSeconds / avged;
            var newTarget = BigIntegerFromBase64(startBlock.Target) * (int)avgSeconds;
            newTarget = newTarget / Configurations.TARGET_SECONDS;
            return newTarget;
        }


    }
}
