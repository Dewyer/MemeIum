using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;

namespace MemeIum.Services.Other
{
    interface IBlockVerifier
    {
        bool Verify(Block block);
    }
}
