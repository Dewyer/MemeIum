using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MemeIum.Services
{
    class Logger : ILogger, IService
    {
        public string LogPath { get; set; }
        public int MinLogLevelToSave { get; set; }
        public int MinLogLevelToDisplay { get; set; }
        public bool InLine = false;
        public string LastLine = "";

        private List<string> LevelPrefixes = new List<string>(){"Info","Warning","Error"};
        private List<string> ToLogQuee;

        public void Init()
        {
            ToLogQuee = new List<string>();
            Task.Run(() => SaverLoop());

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

        private void SaverLoop()
        {
            while (Configurations.MainThreadRunning)
            {
                if (ToLogQuee.Count > 0)
                {
                    try
                    {
                        var nowQuee = new List<string>();
                        nowQuee.AddRange(ToLogQuee);
                        foreach (var log in nowQuee)
                        {
                            File.AppendAllText(LogPath,$"{DateTime.Now.ToString("F")}//{log}");
                            ToLogQuee.Remove(log);
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
                Task.Delay(100).Wait();
            }

        }

        private void AddToSaveQuee(string msg)
        {
            ToLogQuee.Add(msg);
        }

        public void Log(string msg, int level = 0,bool displayInfo = true,bool saveInfo = true,bool show = true)
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
            if (InLine)
            {
                log = "\n" + log;
            }

            InLine = false;
            LastLine = "";
            if (MinLogLevelToDisplay <= level && show)
            {
                Console.WriteLine(log);
            }
            if (MinLogLevelToSave <= level ||saveInfo)
            {
                AddToSaveQuee(log);
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
            InLine = true;
            LastLine = log;

            if (MinLogLevelToDisplay <= level)
            {
                Console.Write(log);
            }
            if (MinLogLevelToSave <= level)
            {
                AddToSaveQuee(log);
            }

        }

        public string LogReadLine()
        {
            InLine = true;
            var cc = Console.ReadLine();
            AddToSaveQuee(cc);
            return cc;
        }
    }
}
