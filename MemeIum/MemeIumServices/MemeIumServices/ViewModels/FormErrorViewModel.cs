using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MemeIumServices.ViewModels
{
    public class RegisterFormErrorViewModel
    {
        public List<string> Errors { get; set; }
        public string Email { get; set; }
        public string PrivKey { get; set; }

    }

    public class LoginFormErrorViewModel
    {
        public List<string> Errors { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }
}
