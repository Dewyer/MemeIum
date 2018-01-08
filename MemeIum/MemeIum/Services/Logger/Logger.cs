using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MemeIum.Services
{
    class Logger : ILogger, IService
    {
        

        public string LogPath { get; set; }
        public int MinLogLevelToSave { get; set; }
        public int MinLogLevelToDisplay { get; set; }

        private List<string> LevelPrefixes = new List<string>(){"Info","Warning","Error"};

        public void Init()
        {
            if (!Directory.Exists(Configurations.CurrentPath+"\\Logs"))
            {
                Directory.CreateDirectory(Configurations.CurrentPath+"\\Logs");
            }
            var date = DateTime.Now.ToString("mm HH dd MM yyyy");
            var revision = 0;
            LogPath = $"{Configurations.CurrentPath}\\Logs\\log-{date}-{revision}.log";

            while (File.Exists(LogPath))
            {
                revision++;
                LogPath = $"{Configurations.CurrentPath}\\Logs\\log-{date}-{revision}.log";
            }

            File.AppendAllText(LogPath,$"Created : {DateTime.Now.ToString("F")}\n");
        }

        public void Log(string msg, int level = 0,bool displayInfo = true)
        {
            if (level < 0 || level >= LevelPrefixes.Count)
            {
                throw new Exception("Invalid loglevel specified.");
            }
            string log = "";
            if (displayInfo)
            {
                log = $"[{LevelPrefixes[level]}]{msg}";
            }
            else
            {
                log = msg;
            }

            if (MinLogLevelToDisplay <= level)
            {
                Console.WriteLine(log);
            }
            if (MinLogLevelToSave <= level)
            {
                File.AppendAllText(LogPath,$"{DateTime.Now.ToString("F")}//{log}\n");
            }
        }

        public void LogPartialLine(string msg, int level = 0, bool displayInfo = true)
        {
            if (level < 0 || level >= LevelPrefixes.Count)
            {
                throw new Exception("Invalid loglevel specified.");
            }
            string log = "";
            if (displayInfo)
            {
                log = $"[{LevelPrefixes[level]}]{msg}";
            }
            else
            {
                log = msg;
            }

            if (MinLogLevelToDisplay <= level)
            {
                Console.Write(log);
            }
            if (MinLogLevelToSave <= level)
            {
                File.AppendAllText(LogPath, $"{DateTime.Now.ToString("F")}//{log}");
            }

        }

        public string LogReadLine()
        {
            var cc = Console.ReadLine();
            File.AppendAllText(LogPath, $"{DateTime.Now.ToString("F")}//{cc}\n");
            return cc;
        }
    }
}
