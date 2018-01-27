using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.ViewModels
{
    public class WalletEssentialInfo
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public float Balance { get; set; }
    }

    public class WalletInfoViewModel
    {
        public string InfoJSON { get; set; }
        public List<WalletEssentialInfo> InfoList { get; set; }
    }
}
