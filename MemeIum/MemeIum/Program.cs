using System;
using MemeIum.Services;

namespace MemeIum
{
    class Program
    {
        private static IP2PServer Server { get; set; }
        private static ILogger Logger { get; set; }

        private static bool RunTests = false;

        static void LoadCommandLineArgs(string[] args)
        {
            if (args.Length != 0)
            {
                int port;
                if (int.TryParse(args[0], out port))
                {
                    Configurations.Config.MainPort = port;
                    Logger.Log($"Changed port: {port}");

                }

                if (args.Length > 1)
                {
                    if (args[1] == "-test")
                    {
                        RunTests = true;
                    }
                }
            }

        }

        static void Main(string[] args)
        {
            Logger = new Logger()
            {
                MinLogLevelToDisplay = Configurations.Config.MinLogLevelToDisplay,
                MinLogLevelToSave = Configurations.Config.MinLogLevelToSave
            };
            Services.Services.RegisterSingeleton(typeof(ILogger),Logger);
            
            LoadCommandLineArgs(args);

            Services.Services.RegisterSingeleton(typeof(IMappingService),new MappingService());

            Server = new P2PServer();
            Services.Services.RegisterSingeleton(typeof(IP2PServer), Server);
            Server.Start();
            Logger.Log("Starting up node...");

            if (RunTests)
            {
                var mck = new MockClient();
                mck.MockTest();
            }

            Console.ReadLine();
        }
    }
}
