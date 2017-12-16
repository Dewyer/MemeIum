using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using Newtonsoft.Json;
using System.IO;

namespace MemeIum.Services
{
    static class Configurations
    {
        public static Config Config { get; set; }

        static Configurations()
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("./Settings.json"));
        }
    }
}
