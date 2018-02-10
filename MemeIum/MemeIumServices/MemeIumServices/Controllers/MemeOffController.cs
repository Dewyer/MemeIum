using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumServices.Controllers
{
    public class MemeOffController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
    }
}
