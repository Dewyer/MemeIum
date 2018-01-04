using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;

namespace MemeIum.Services.Watcher
{
    interface IWatcherService
    {
        void OnNewBlockHandler(object block);
    }
}
