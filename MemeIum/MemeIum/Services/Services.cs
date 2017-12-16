using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Services
{
    public static class Services
    {
        private static Dictionary<Type,object> _services = new Dictionary<Type, object>();

        public static void RegisterSingeleton(Type type,object target)
        {
            _services.Add(type,target);
        }

        public static T GetService<T>()
        {
            if (_services.ContainsKey(typeof(T)))
                return (T)_services[typeof(T)];
            else
            {
                return default(T);
            }
        }

    }
}
