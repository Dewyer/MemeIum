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

        static Configurations()
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText($"{CurrentPath}\\Settings.json"));
        }
    }
}
