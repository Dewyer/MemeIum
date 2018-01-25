using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumServices.Services
{
    public interface IAuthService
    {
        IActionResult AuthenticatedResponse(IActionResult correct,IActionResult bad=null);
        IActionResult Login(IFormCollection form);
        IActionResult Register(IFormCollection form);
    }

    public class AuthService : IAuthService
    {
        private UASContext context;

        public AuthService(UASContext _context)
        {
            context = _context;

        }

        public bool TryRegister(IFormCollection form,out FormErrorViewModel error)
        {
            return true;
        }

        public IActionResult AuthenticatedResponse(IActionResult correct, IActionResult bad = null)
        {
            return new ObjectResult("not implen");
        }

        public IActionResult Login(IFormCollection form)
        {
            return new RedirectResult("/Home/Index");
        }

        public IActionResult Register(IFormCollection form)
        {
            var regSuccess = TryRegister(form, out FormErrorViewModel errors);

            return new RedirectToActionResult("Register","Home",routeValues:errors);
        }
    }
}
