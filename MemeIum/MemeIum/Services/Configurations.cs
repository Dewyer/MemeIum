﻿using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace MemeIum.Services
{
    static class Configurations
    {
        public static Config Config { get; set; }
        public static string CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public const int MAX_TRANSACTION_SIZE_BYTES = 3000;
        public const int MAX_BLOCK_SIZE_BYTES = 12601200;

        public const string GENESIS_BLOCK_ID = "AMeIyz3TYv5NoAxY4zRmaZ4eAbY+uKQvewFofZFOqfM=";

        static Configurations()
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText($"{CurrentPath}\\Settings.json"));
        }
    }
}
