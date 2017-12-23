﻿using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MemeIum.Misc
{
    class Config
    {
        public string Version { get; set; }

        public int MainPort { get; set; }
        public int MaxPeersGiven { get; set; }
        public int MaxPeersHave { get; set; }
        public int SecondsToWaitForAddresses { get; set; }

        public int MinLogLevelToSave { get; set; }
        public int MinLogLevelToDisplay { get; set; }

    }
}
