using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services.Blockchain;
using MemeIum.Services.Eventmanagger;

namespace MemeIum.Services.Other
{
    class DifficultyService : IDifficultyService
    {
        private readonly IBlockChainService _blockChainService;

        public DifficultyService()
        {
            _blockChainService = Services.GetService<IBlockChainService>();
        }

        public BigInteger TargetForBlock(BlockInfo info)
        {
            var atblockInfo =info;
            var totalTime = 0d;
            var avged = 0;
            var heightStart = atblockInfo.Height - (atblockInfo.Height % Configurations.MA_SIZE_FOR_TARGET);
            for (int ii = 0; ii < Configurations.MA_SIZE_FOR_TARGET; ii++)
            {
                var lastTime = atblockInfo.CreationTime;
                atblockInfo = _blockChainService.LookUpBlockInfoByHeight(heightStart-ii);
                var elapsed = atblockInfo.CreationTime.Subtract(lastTime).TotalSeconds;

                if (elapsed < Configurations.MAX_SECONDS_FOR_TARGET_AVERAGE)
                {
                    totalTime += elapsed;
                    avged++;
                }

                if (atblockInfo.Id == Configurations.GENESIS_BLOCK_ID)
                {
                    break;
                }
            }

            var avgTicks = (long)((totalTime / avged) * 10000000);
            var oldTargetStr = _blockChainService.LookUpBlockInfoByHeight(heightStart).Target;
            var oldTargetInt = new BigInteger(Convert.FromBase64String(oldTargetStr));
            var newTarget = (oldTargetInt * avgTicks)/Configurations.TARGET_SECONDS;

            return newTarget;
        }


    }
}
