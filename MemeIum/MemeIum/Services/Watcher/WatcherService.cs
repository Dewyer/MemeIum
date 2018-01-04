using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Services.Eventmanagger;

namespace MemeIum.Services.Watcher
{
    class WatcherService : IWatcherService
    {
        private readonly ILogger _logger;

        public WatcherService()
        {
            var ev = Services.GetService<IEventManager>();
            ev.RegisterEventListener(OnNewBlockHandler,EventTypes.EventType.NewBlock);

            _logger = Services.GetService<ILogger>();
        }

        public void OnNewBlockHandler(object block)
        {
            var blockReal = (Block) block;
            _logger.Log("New block to watch! "+blockReal.Body.Id);
        }
    }
}
