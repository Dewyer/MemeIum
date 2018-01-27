using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Models;
using MemeIumServices.Services;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MemeIumServices.ViewComponents
{
    [ViewComponent]
    public class WalletInfoViewComponent : ViewComponent
    {
        private UASContext context;
        private INodeComService nodeComService;

        public WalletInfoViewComponent(UASContext context, INodeComService nodeComService)
        {
            this.context = context;
            this.nodeComService = nodeComService;
        }

        public List<WalletEssentialInfo> GetInfosForUser(User usr)
        {
            context.Database.EnsureCreated();
            nodeComService.UpdatePeers();

            var eInfos = new List<WalletEssentialInfo>();
            var wallets = context.Wallets.Where(r=>r.OwnerId == usr.UId);

            foreach (var wallet in wallets)
            {
                var unspent =nodeComService.GetUnspentVOutsForAddress(wallet.Address);
                var bb = 0f;
                if (unspent != null)
                {
                    bb = unspent.Sum(r => r.Amount) / 100000f;
                }

                eInfos.Add( new WalletEssentialInfo(){Name = wallet.Name,Address = wallet.Address,Balance =bb });
            }
            return eInfos;

        }

        public async Task<IViewComponentResult> InvokeAsync(User usr)
        {
            var eInfos = GetInfosForUser(usr);
            var js = JsonConvert.SerializeObject(eInfos);
            var walletInfo = new WalletInfoViewModel()
            {
                InfoJSON = js,
                InfoList = eInfos
            };
            return View(walletInfo);
        }

    }
}
