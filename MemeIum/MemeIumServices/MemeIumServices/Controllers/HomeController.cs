using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using Microsoft.AspNetCore.Mvc;
using MemeIumServices.Models;
using MemeIumServices.Models.Transaction;
using MemeIumServices.Services;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace MemeIumServices.Controllers
{
    public class HomeController : Controller
    {
        private INodeComService nodeCom;
        private IAuthService authService;
        private UASContext context;
        private IWalletUtil walletUtil;

        public HomeController(INodeComService _nodecom,IAuthService _authService, UASContext context, IWalletUtil walletUtil)
        {
            nodeCom = _nodecom;
            authService = _authService;
            this.context = context;
            this.walletUtil = walletUtil;
        }

        public IActionResult Test()
        {
            var qr = walletUtil.SaveQrCode("asd");
            return new ObjectResult(new {resp=qr});
        }

        [HttpGet]
        public IActionResult Wallet()
        {
            var model = new WalletViewModel()
            {
                User = authService.GetAuthUser(Request,Response)
            };
            return authService.AuthenticatedResponse(Request,Response,View(model));
        }

        [HttpPost]
        public IActionResult Send()
        {
            nodeCom.UpdatePeers();
            var usr = authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return authService.UnAuthenticatedResult;
            }
            var from = "";
            if (Request.Form.ContainsKey("wallet"))
            {
                if (Request.Form["wallet"].ToString().Split('-').Length > 1)
                {
                    from = Request.Form["wallet"].ToString().Split('-')[1];
                }
                else
                {
                    return authService.UnAuthenticatedResult;
                }
            }
            var wallet = context.Wallets.First(r => r.OwnerId == usr.UId && r.Address == from);
            if (wallet == null)
            {
                return authService.UnAuthenticatedResult;
            }
            var oo = nodeCom.SendTransaction(usr,wallet,Request.Form);
            var suc = new ActionResultViewModel();
            if (oo != null)
            {
                walletUtil.SaveToHistory(oo,usr);
                suc.Title = "Success !";
                suc.SubTitle = "Transaction sent successfully!";
                suc.TextClass = "text-success";
                suc.ButtonClass = "btn-success";
                suc.ActionButtonLink = "/Home/Wallet";
                suc.ActionButtonText = "Back to wallet";

                return new RedirectToActionResult("Result", "Home", routeValues:suc);
            }
            else
            {
                suc.Title = "Error !";
                suc.SubTitle = "Transaction was not sent successfully!";
                suc.TextClass = "text-danger";
                suc.ButtonClass = "btn-danger";
                suc.ActionButtonLink = "/Home/Wallet";
                suc.ActionButtonText = "Back to wallet";

                return new RedirectToActionResult("Result", "Home", routeValues:suc);
            }
        }

        [HttpGet]
        public IActionResult MakeRequest()
        {
            var usr = authService.GetAuthUser(Request,Response);
            if (usr == null)
            {
                return authService.UnAuthenticatedResult;
            }
            var model = new ActiveUserViewModel()
            {
                User = usr
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult DoMakeRequest()
        {
            var usr = authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return authService.UnAuthenticatedResult;
            }

            if (!(Request.Form.ContainsKey("msgp") && Request.Form.ContainsKey("msgt") &&
                  Request.Form.ContainsKey("amm") && Request.Form.ContainsKey("wallet")))
            {
                return authService.UnAuthenticatedResult;
            }

            var qr = QueryHelpers.AddQueryString("http://memeium.azurewebsites.net/Home/PayRequest", "msgp",
                Request.Form["msgp"]);
            qr = QueryHelpers.AddQueryString(qr, "msgt", Request.Form["msgt"]);
            qr = QueryHelpers.AddQueryString(qr, "amm", Request.Form["amm"].ToString());
            qr = QueryHelpers.AddQueryString(qr, "addr", Request.Form["wallet"].ToString().Split('-')[1]);

            var qrCode = walletUtil.SaveQrCode(qr);
            var model = new RequestOutputViewModel()
            {
                Title = Request.Form["msgp"],
                Message = Request.Form["msgt"],
                FileNameOfQr = qrCode+".png",
                Url=qr,
                ToAddr = Request.Form["wallet"].ToString().Split('-')[1],
                Ammount = Request.Form["amm"]
            };
            return View(model);
        }

        public IActionResult PayRequest(string msgp,string msgt,string amm,string addr)
        {
            if (msgp == null)
            {
                msgp = "";
            }
            if (msgt == null)
            {
                msgt = "";
            }
            if (amm == null)
            {
                amm = "0";
            }
            if (addr == null)
            {
                return NotFound();
            }
            var usr = authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return authService.UnAuthenticatedResult;
            }

            var req = new RequestOutputViewModel()
            {
                User = usr,
                Ammount = amm,
                FileNameOfQr = "",
                Message = msgt,
                Title = msgp,
                ToAddr = addr,
                Url=""
            };
            return View(req);
        }

        [HttpGet]
        public IActionResult Result(ActionResultViewModel model)
        {
            if (model == null)
            {
                return new RedirectResult("/Home/Index");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(LoginFormErrorViewModel model)
        {
            if (model == null)
            {
                return View(new LoginFormErrorViewModel() {Email = "", Errors = new List<string>(),Message = ""});
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult DoLogin()
        {
            return authService.Login(Request,Response);
        }

        [HttpGet]
        public IActionResult Register(RegisterFormErrorViewModel model)
        {
            if (model == null)
            {
                return View(new RegisterFormErrorViewModel() {Errors = new List<string>(),Email = "",PrivKey = ""});
            }
            return View(model);
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

        [HttpGet]
        public IActionResult AddWallet()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DoAddWallet()
        {
            var usr = authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return authService.UnAuthenticatedResult;
            }
            var badRes = new ActionResultViewModel()
            {
                Title = "Failure!",
                SubTitle = "Wallet was not added!",
                ActionButtonLink = "/Home/AddWallet/",
                ActionButtonText = "Back",
                ButtonClass = "btn-danger",
                TextClass = "text-danger"
            };

            if (!Request.Form.ContainsKey("privkey") || !Request.Form.ContainsKey("name"))
            {
                return new RedirectToActionResult("Result", "Home", routeValues: badRes);
            }
            var key = Request.Form["privkey"].ToString();
            var name = Request.Form["name"].ToString();

            var suc = authService.AddWallet(key,name,usr);

            if (suc)
            {
                var res = new ActionResultViewModel()
                {
                    Title = "Success!",
                    SubTitle = "Wallet added successfully!",
                    ActionButtonLink = "/Home/Wallet/",
                    ActionButtonText = "Go to wallet",
                    ButtonClass = "btn-success",
                    TextClass = "text-success"
                };
                return new RedirectToActionResult("Result", "Home", routeValues: res);
            }
            else
            {
                return new RedirectToActionResult("Result", "Home", routeValues: badRes);
            }
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
