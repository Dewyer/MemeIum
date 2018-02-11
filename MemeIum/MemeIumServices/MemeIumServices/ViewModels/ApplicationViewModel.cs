using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MemeIumServices.ViewModels
{
    public class ApplicationViewModel
    {
        public string Wallet { get; set; }
        public string Title { get; set; }
        public IFormFile Image { get; set; }
    }
}
