using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MemeIum.Services.Other
{
    interface IDifficultyService
    {
        BigInteger CurrentTarget { get; set; }


    }
}
