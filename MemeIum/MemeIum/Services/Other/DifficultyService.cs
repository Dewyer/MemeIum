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

        public BigInteger TargetForBlock(BlockInfo info)
        {
            var max = BigInteger.Pow(2, 256);
            //want 2000 hashes to run
            //chances to roll good = (max-target)/max * 2000 = 1, or 1999x / 2000 = y
            var target = BigInteger.Divide(max, 3888000);
            var maxVaStr = Convert.ToBase64String(target.ToByteArray());

            var first = maxVaStr;
            var bb = new BigInteger(Convert.FromBase64String(first));
            Console.WriteLine(max.ToString("R"));
            Console.WriteLine(bb.ToString("R"));
            return bb;

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
