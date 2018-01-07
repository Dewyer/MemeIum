using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Services
{
    public static class Services
    {
        private static Dictionary<Type,object> _services = new Dictionary<Type, object>();
        private static List<Type> servicesTypes = new List<Type>();
        private static List<Type> interFaceTypes = new List<Type>();

        public static void RegisterSingeleton<T,T2>()
        {
            interFaceTypes.Add(typeof(T));
            servicesTypes.Add(typeof(T2));
        }

        public static void Initialize()
        {
            for (int ii = 0; ii < interFaceTypes.Count;ii++)
            {
                var obj = Activator.CreateInstance(servicesTypes[ii]);
                _services.Add(interFaceTypes[ii],obj);
            }

            foreach (var service in _services)
            {
                ((IService)service.Value).Init();
            }
        }

        public static T GetService<T>()
        {
            if (_services.ContainsKey(typeof(T)))
                return (T)_services[typeof(T)];
            else
            {
                throw new Exception("No service");
            }
        }

    }
}

