using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MemeIumServices.ViewModels
{
    public class FormErrorViewModel
    {
        public List<string> Errors { get; set; }
        public IFormCollection OldForm { get; set; }
    }
}
