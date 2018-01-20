using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MemeIumServices.Models;
using MemeIumServices.Services;

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
            var from = Request.Form["addr"].ToString();

            if (true)
            {
                
                return new RedirectResult("Wallet");
            }

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
