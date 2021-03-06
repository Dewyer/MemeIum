﻿using System;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Threading;
using MemeIum.Services;
using MemeIum.Services.Blockchain;
using MemeIum.Services.CatchUp;
using MemeIum.Services.EmbededWebServer;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.EventManager;
using MemeIum.Services.Mineing;
using MemeIum.Services.Other;
using MemeIum.Services.UI;
using MemeIum.Services.Wallet;

namespace MemeIum
{
    public class Program
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
                    Console.WriteLine($"Changed port {port}");

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

        public static void Main(string[] args)
        {
            Configurations.MainThreadRunning = true;

            LoadCommandLineArgs(args);
            Services.Services.RegisterSingeleton<ILogger,Logger>();
            Services.Services.RegisterSingeleton<IEventManager,EventManager>();

            Services.Services.RegisterSingeleton<IBlockVerifier,BlockVerifier>();
            Services.Services.RegisterSingeleton<IBlockChainService,BlockChainService>();
            Services.Services.RegisterSingeleton<IDifficultyService,DifficultyService>();
            Services.Services.RegisterSingeleton<IMinerService,MinerService>();

            Services.Services.RegisterSingeleton<ITransactionVerifier,TransactionVerifier>();
            Services.Services.RegisterSingeleton<IWalletService,WalletService>();
            Services.Services.RegisterSingeleton<IP2PServer,P2PServer>();
            Services.Services.RegisterSingeleton<IMappingService,MappingService>();
            Services.Services.RegisterSingeleton<ICatchUpService,CatchUpService>();
            Services.Services.RegisterSingeleton<IEmbededWebServer,EmbededWebServer>();
            Services.Services.RegisterSingeleton<IUI,Ui>();
            Services.Services.Initialize();

            var Logger = Services.Services.GetService<ILogger>();
            Logger.MinLogLevelToSave = Configurations.Config.MinLogLevelToSave;
            Logger.MinLogLevelToDisplay = Configurations.Config.MinLogLevelToDisplay;
            var mck = new MockClient();
            //mck.GenerateRandomWallets(100);
            //mck.CreateGenesis();
            //mck.FirstTarget();
            //mck.CreateNewOrigins();
            Configurations.MainThreadRunning = false;
            Thread.Sleep(400);
            Environment.Exit(0);
        }
    }
}

