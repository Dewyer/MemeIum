using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MemeIum.Services.Eventmanagger;

namespace MemeIum.Services.EventManager
{
    class EventManager : IEventManager
    {
        private Dictionary<EventTypes.EventType, List<Action<object>>> _handlers;

        public EventManager()
        {
            _handlers = new Dictionary<EventTypes.EventType, List<Action<object>>>();
            _handlers.Add(EventTypes.EventType.NewBlock,new List<Action<object>>());
            _handlers.Add(EventTypes.EventType.NewTransaction,new List<Action<object>>());
        }

        public void RegisterEventListener(Action<object> action, EventTypes.EventType type)
        {
            _handlers[type].Add(action);
        }

        public void PassNewTrigger(object obj, EventTypes.EventType type)
        {
            foreach (var action in _handlers[type])
            {
                Task.Run(()=>action(obj));
            }
        }
    }
}
