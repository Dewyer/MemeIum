using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MemeIum.Misc.UI;
using MemeIum.Services.Mineing;
using MemeIum.Services.Other;
using MemeIum.Services.Wallet;
using Newtonsoft.Json;

namespace MemeIum.Services.UI
{
    class Ui : IUI, IService
    {
        private ILogger _logger;
        private IP2PServer _server;
        private IMinerService _minerService;
        private ITransactionVerifier _transactionVerifier;
        private IWalletService _walletService;

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
            var balance = vouts.Sum(r => r.Amount);

            _logger.Log(GetIndentTabs()+$"Address : {addr} , Balance : {balance}",displayInfo:false);
        }

        public void StartWalletLoop()
        {
            InIndent++;

            while (true)
            {
                LogCmdInputToIndent();
                var cmdBody = _logger.LogReadLine().ToLower();
                if (cmdBody == "balance")
                {
                    ShowBalance();
                }
                else if (cmdBody == "help")
                {
                    _logger.Log(GetIndentTabs()+res.HelpWalletText,displayInfo:false);
                }
                else if (cmdBody == "b")
                {
                    break;
                }
            }

            InIndent--;
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
