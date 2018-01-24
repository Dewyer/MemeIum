using System;
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
        public static bool MainThreadRunning { get; set; }

        public const int MAX_TRANSACTION_SIZE_BYTES = 3000;
        public const int MAX_BLOCK_SIZE_BYTES = 12601200;
        public const int MA_SIZE_FOR_TARGET = 720;
        public const int MAX_SECONDS_FOR_TARGET_AVERAGE = 60*30;
        public const int TARGET_SECONDS = 120;
        public const int TRANSACTION_WANT_LIMIT = 20;
        public const int BLOCK_REWARD = 420000;
        public const int CATCHUP_N = 5;
        public const int MAX_STR_LEN = 50;
        public const float MAX_TIME_BETWEEN_CATCHUP_RESP = 10;

        public const string GENESIS_BLOCK_ID = "sCOL48pO0Gbwo2sqqRe7nIoRrHIRBdAZILaIxarHBKA=";

        static Configurations()
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText($"{CurrentPath}\\Settings.json"));
        }
    }
}
