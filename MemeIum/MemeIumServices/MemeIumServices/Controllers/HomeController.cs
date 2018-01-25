using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
        private IAuthService authService;

        public HomeController(INodeComService _nodecom,IAuthService _authService)
        {
            nodeCom = _nodecom;
            authService = _authService;
        }

        [HttpGet]
        public IActionResult Wallet()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Send()
        {
            nodeCom.UpdatePeers();
            var oo = nodeCom.SendTransaction(Request.Form);
            if (oo)
            {
                return new ObjectResult(new {succ="Transaction succesfully delivered"});
            }
            return new ObjectResult(new { error = true });
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DoLogin()
        {
            return authService.Login(Request.Form);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DoRegister()
        {
            return authService.Register(Request.Form);
        }

        [HttpGet]
        public IActionResult GenerateWallet()
        {
            var rsa = new RSACryptoServiceProvider(2000);
            var priv = rsa.ExportParameters(true);
            var sw = new System.IO.StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, priv);

            return View(model:sw.ToString());
        }

        public IActionResult GetBalance(string address)
        {
            return ViewComponent("GetBalance", new { addr = address });
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
