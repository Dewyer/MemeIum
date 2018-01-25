using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MemeIum.Misc.UI;
using MemeIum.Services.Mineing;
using MemeIum.Services.Other;
using MemeIum.Services.Wallet;
using Newtonsoft.Json;
using MemeIum.Misc.Transaction;
using MemeIum.Misc;
using MemeIum.Requests;
using MemeIum.Services.Blockchain;
using MemeIum.Services.CatchUp;
using MemeIum.Services.Eventmanagger;
using Unosquare.Swan;

namespace MemeIum.Services.UI
{
    class Ui : IUI, IService
    {
        private ILogger _logger;
        private IP2PServer _server;
        private IMinerService _minerService;
        private ITransactionVerifier _transactionVerifier;
        private IWalletService _walletService;
        private IEventManager _eventManager;
        private IMappingService _mappingService;
        private ICatchUpService _catchUpService;
        private IBlockChainService _blockChainService;

        private int _indent;
        private int InIndent
        {
            get => _indent;
            set
            {
                if (value >= 0)
                {
                    _indent = value;
                }
            }
        }

        private TextResources res;
        public void Init()
        {
            _logger = Services.GetService<ILogger>();
            _server = Services.GetService<IP2PServer>();
            _minerService = Services.GetService<IMinerService>();
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _walletService = Services.GetService<IWalletService>();
            _eventManager = Services.GetService<IEventManager>();
            _mappingService = Services.GetService<IMappingService>();
            _catchUpService = Services.GetService<ICatchUpService>();
            _blockChainService = Services.GetService<IBlockChainService>();

            var textRes = $"{Configurations.CurrentPath}\\TextResources.json";
            res = JsonConvert.DeserializeObject<TextResources>(File.ReadAllText(textRes));

            StartMainLoop();
        }

        private string GetIndentTabs()
        {
            var tabs = "";
            for (int ii = 0; ii < InIndent; ii++)
            {
                tabs += "\t";
            }
            return tabs;
        }

        private void LogCmdInputToIndent()
        {
            _logger.LogPartialLine(GetIndentTabs()+">>>",displayInfo:false);
        }

        public void ShutDown()
        {
            //Stop server and wait
            _logger.Log("Shuting Down");
            foreach (var ww in _minerService.CurrentWorkers)
            {
                ww.Abort();
            }
            _logger.Log(".. Bye Bye");
        }

        public void ShowBalance()
        {
            var addr = _walletService.Address;
            var vouts = _transactionVerifier.GetAllTransactionVOutsForAddress(addr);
            var balance = vouts.Sum(r => r.Amount) / 100000f;

            _logger.Log(GetIndentTabs()+$"Address : {addr} , Balance : {balance} Memeium",displayInfo:false);
        }

        public void SendTShort(string to, float ammount,string msg)
        {

            var trans = _walletService.AssembleTransaction(to, ammount, msg);
            if (trans == null)
                return;

             _eventManager.PassNewTrigger(trans, EventTypes.EventType.NewTransaction);
        }

        public void StartWalletLoop()
        {
            InIndent++;

            while (true)
            {
                LogCmdInputToIndent();
                var cmdBody = _logger.LogReadLine().ToLower();
                var tokens = cmdBody.Split(' ');
                if (cmdBody == "balance")
                {
                    ShowBalance();
                }
                else if (tokens[0] == "sendtshort" && tokens.Length == 4)
                {

                    var am = float.Parse(tokens[3],new CultureInfo("en-US"));
                    SendTShort(tokens[1], am,tokens[2]);
                }
                else if (cmdBody == "help")
                {
                    _logger.Log(GetIndentTabs() + res.HelpWalletText, displayInfo: false);
                }
                else if (cmdBody == "b")
                {
                    break;
                }
                else
                {
                    _logger.Log(GetIndentTabs() + "Invalid command, for help type 'help'.", displayInfo: false);
                }
            }

            InIndent--;
        }

        public void Pings()
        {
            var obj = new RequestHeader()
            {
                Version = Configurations.Config.Version,
                Type = -1,
                Sender = RequestHeader.Me
            };
            foreach (var peer in _mappingService.Peers)
            {
                Console.WriteLine($"Pinged : {peer.Address}|{peer.Port}");
                _server.SendResponse(obj,peer);
            }
        }

        public void AddIp(string[] tokens)
        {
            if (tokens.Length == 3)
            {
                try
                {
                    var addr = tokens[1];
                    var port = int.Parse(tokens[2]);
                    var peer = new Peer()
                    {
                        Address = addr,
                        Port = port
                    };
                    _mappingService.RegisterPeer(peer);
                }
                catch
                {
                    _logger.Log("Could not add ip..",displayInfo:false);
                }
            }
        }

        public void ShowPeers()
        {
            _logger.Log(res.Separator, displayInfo: false);
            _logger.Log("You'r peers :", displayInfo: false);
            foreach (var peer in _mappingService.Peers)
            {
                _logger.Log($"Address : {peer.Address} , Port : {peer.Port}", displayInfo: false);
            }

            _logger.Log(res.Separator, displayInfo: false);

        }

        public void ReachDownTest()
        {
            var block = _blockChainService.LookUpBlockInfo(_blockChainService.Info.EndOfLongestChain);
            var last = block.Stringify();

            while (block.LastBlockId != Configurations.GENESIS_BLOCK_ID)
            {
                last = block.Stringify();
                block = _blockChainService.LookUpBlockInfo(block.LastBlockId);
                if (block == null)
                {
                    Console.WriteLine("[ReachDown]Chain does not reach down.");
                    Console.WriteLine(last);
                    break;
                }
            }
            Console.WriteLine("[ReachDown]Chain does reach down.");

        }

        public void RunBlockchainTest()
        {
            _logger.Log($"Block chain test running ..");
            ReachDownTest();
        }

        public void StartMainLoop()
        {
            _logger.Log("Starting Console UI...");
            _logger.Log(res.Separator,displayInfo: false);
            _logger.Log(asciArt, displayInfo: false);
            _logger.Log(res.Separator, displayInfo: false);
            _logger.Log("Type 'help' for help.", displayInfo: false);

            while (true)
            {
                LogCmdInputToIndent();
                var cmdBody = _logger.LogReadLine().ToLower();
                var cmdTokens = cmdBody.Split(' ');

                if (cmdBody.StartsWith("help"))
                {
                    _logger.Log(res.HelpText, displayInfo: false);
                }
                else if (cmdBody.StartsWith("wallet"))
                {
                    StartWalletLoop();
                }
                else if (cmdBody.StartsWith("pings"))
                {
                    Pings();
                }
                else if (cmdBody.StartsWith("addip"))
                {
                    AddIp(cmdTokens);
                }
                else if (cmdBody.StartsWith("btest"))
                {
                    RunBlockchainTest();
                }
                else if (cmdBody.StartsWith("showp"))
                {
                    ShowPeers();
                }
                else if (cmdBody == "q" || cmdBody == "quit")
                {
                    break;
                }
                else
                {
                    _logger.Log("Invalid command, type 'help' for help.", displayInfo: false);
                }
            }

            ShutDown();
        }





        private const string asciArt = @"
      ___           ___           ___           ___                       ___           ___     
     /\  \         /\__\         /\  \         /\__\                     /\  \         /\  \    
    |::\  \       /:/ _/_       |::\  \       /:/ _/_       ___          \:\  \       |::\  \   
    |:|:\  \     /:/ /\__\      |:|:\  \     /:/ /\__\     /\__\          \:\  \      |:|:\  \  
  __|:|\:\  \   /:/ /:/ _/_   __|:|\:\  \   /:/ /:/ _/_   /:/__/      ___  \:\  \   __|:|\:\  \ 
 /::::|_\:\__\ /:/_/:/ /\__\ /::::|_\:\__\ /:/_/:/ /\__\ /::\  \     /\  \  \:\__\ /::::|_\:\__\
 \:\~~\  \/__/ \:\/:/ /:/  / \:\~~\  \/__/ \:\/:/ /:/  / \/\:\  \__  \:\  \ /:/  / \:\~~\  \/__/
  \:\  \        \::/_/:/  /   \:\  \        \::/_/:/  /   ~~\:\/\__\  \:\  /:/  /   \:\  \      
   \:\  \        \:\/:/  /     \:\  \        \:\/:/  /       \::/  /   \:\/:/  /     \:\  \     
    \:\__\        \::/  /       \:\__\        \::/  /        /:/  /     \::/  /       \:\__\    
     \/__/         \/__/         \/__/         \/__/         \/__/       \/__/         \/__/    
 ";
    }
}
