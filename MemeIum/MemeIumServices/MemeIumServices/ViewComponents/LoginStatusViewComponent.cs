using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models;
using MemeIumServices.Services;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MemeIumServices.ViewComponents
{
    public class LoginStatusViewComponent : ViewComponent
    {
        private IAuthService authService;

        public LoginStatusViewComponent(IAuthService authService)
        {
            this.authService = authService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var usr = authService.GetAuthUser(Request, HttpContext.Response);
            return View(usr);
        }


    }
}
