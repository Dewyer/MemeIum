using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MemeIum.Services
{
    class Logger : ILogger
    {
        

        public string LogPath { get; set; }
        public int MinLogLevelToSave { get; set; }
        public int MinLogLevelToDisplay { get; set; }

        private List<string> LevelPrefixes = new List<string>(){"Info","Warning","Error"};

        public Logger()
        {
            if (!Directory.Exists("./Logs"))
            {
                Directory.CreateDirectory("./Logs");
            }
            var date = DateTime.Now.ToString("mm HH dd MM yyyy");
            var revision = 0;
            LogPath = $"./Logs/log-{date}-{revision}.log";

            while (File.Exists(LogPath))
            {
                revision++;
                LogPath = $"./Logs/log-{date}-{revision}.log";
            }

            File.AppendAllText(LogPath,$"Created : {DateTime.Now.ToString("F")}\n");
        }

        public void Log(string msg, int level = 0)
        {
            if (level < 0 || level >= LevelPrefixes.Count)
            {
                throw new Exception("Invalid loglevel specified.");
            }
            var log = $"[{LevelPrefixes[level]}]{msg}";

            if (MinLogLevelToDisplay <= level)
            {
                Console.WriteLine(log);
            }
            if (MinLogLevelToSave <= level)
            {
                File.AppendAllText(LogPath,$"{DateTime.Now.ToString("F")}//{log}");
            }
        }
    }
}
