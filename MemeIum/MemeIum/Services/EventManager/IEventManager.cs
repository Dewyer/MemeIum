using MemeIum.Misc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using MemeIum.Requests;

namespace MemeIum.Services.Eventmanagger
{
    public static class EventTypes
    {
        public enum EventType
        {
            NewBlock,
            NewTransaction
        }
    }

    interface IEventManager
    {
        void RegisterEventListener(Action<object> action,EventTypes.EventType type);
        void PassNewTrigger(object obj, EventTypes.EventType type);
    }
}
