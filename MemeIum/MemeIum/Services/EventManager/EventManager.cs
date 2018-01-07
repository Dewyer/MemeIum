using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MemeIum.Services.Eventmanagger;

namespace MemeIum.Services.EventManager
{
    class EventManager : IEventManager, IService
    {
        private Dictionary<EventTypes.EventType, List<Action<object>>> _handlers;

        public void Init()
        {
            _handlers = new Dictionary<EventTypes.EventType, List<Action<object>>>();
            foreach (EventTypes.EventType val in Enum.GetValues(typeof(EventTypes.EventType)))
            {
                _handlers.Add(val, new List<Action<object>>());
            }
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
