using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MemeIum.Misc;

namespace MemeIum.Services.Other
{
    interface IDifficultyService
    {
        BigInteger TargetForBlock(BlockInfo info);

    }
}
