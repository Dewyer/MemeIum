using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models;

namespace MemeIumServices.ViewModels
{
    public class RequestOutputViewModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ToAddr { get; set; }
        public string Ammount { get; set; }
        public string Url { get; set; }
        public string FileNameOfQr { get; set; }
        public User User { get; set; }
    }
}
