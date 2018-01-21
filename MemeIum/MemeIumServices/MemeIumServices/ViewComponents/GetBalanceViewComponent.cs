using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumServices.ViewComponents
{
    [ViewComponent]
    public class GetBalanceViewComponent : ViewComponent
    {
        private INodeComService nodeCom;

        public GetBalanceViewComponent(INodeComService _nodeCom)
        {
            nodeCom = _nodeCom;
        }

        public async Task<IViewComponentResult> InvokeAsync(string addr)
        {
            nodeCom.UpdatePeers();
            var unspent = nodeCom.GetUnspentVOutsForAddress(addr);
            if (unspent.Count == 0)
            {
                return View(0f);
            }
            var balance = unspent.Sum(r=>r.Amount) / 100000f;
            return View(balance);
        }
    }
}
