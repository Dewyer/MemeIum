using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MemeIumServices.Models;
using MemeIumServices.Models.Transaction;
using MemeIumServices.Services;
using Microsoft.AspNetCore.Hosting.Internal;

namespace MemeIumServices.Controllers
{
    public class HomeController : Controller
    {
        private INodeComService nodeCom;

        public HomeController(INodeComService _nodecom)
        {
            nodeCom = _nodecom;
        }

        [HttpGet]
        public IActionResult Wallet()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Send()
        {
            Transaction trans = null;
            //nodeCom.UpdatePeers();
            //var tt = nodeCom.RequestToRandomPeer($"api/getbalance/Ya5HUuXRT79VA4IL0FlM7DXtOeXcdaWsas68bzY3zDs=");
            //tt.Wait();
            //return new ObjectResult(tt.Result);
            try
            {
                trans = nodeCom.GetTransactionFromForm(Request.Form);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { error = ex.Message+"    "+ex.StackTrace });
            }
            if (trans != null)
            {
                var oo = nodeCom.SendTransaction(trans);
                return new ObjectResult(new { error = oo });
            }
            return new ObjectResult(new { error =true });
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
