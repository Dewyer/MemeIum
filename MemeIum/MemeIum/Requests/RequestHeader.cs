using System;
using System.Collections.Generic;
using System.Text;

namespace MemeIum.Requests
{
    class RequestHeader
    {
        public static Dictionary<Type, int> RequestIndexes;

        static RequestHeader()
        {
            RequestIndexes = new Dictionary<Type, int>();

            //Types
            RequestIndexes.Add(typeof(GetAddressesRequest),0);
            RequestIndexes.Add(typeof(AddressesRequest), 1);
            RequestIndexes.Add(typeof(TransactionRequest), 2);

        }

        public string Version { get; set; }
        public int Type { get; set; }
        
    }
}
