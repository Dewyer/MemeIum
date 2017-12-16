using System;
using MemeIum.Services;

namespace MemeIum
{
    class Program
    {
        private static IP2PServer Server { get; set; }

        static void Main(string[] args)
        {
            Services.Services.RegisterSingeleton(typeof(IMappingService),new MappingService());

            Server = new P2PServer();
            Services.Services.RegisterSingeleton(typeof(IP2PServer), Server);
            Server.Start();
            var mckClient = new MockClient();

            mckClient.MockTest1();

            Console.ReadLine();
        }
    }
}
