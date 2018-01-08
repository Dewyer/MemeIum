using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Services
{
    interface ILogger
    {
        string LogPath { get; set; }
        int MinLogLevelToSave { get; set; }
        int MinLogLevelToDisplay { get; set; }
        void Log(string msg, int level = 0,bool displayInfo = true);
        void LogPartialLine(string msg, int level = 0, bool displayInfo = true);
        string LogReadLine();
    }
}
