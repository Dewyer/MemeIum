using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public class Wallet
    {
        public string KeyString { get; set; }
        [Key]
        public string Address { get; set; }

        public User User { get; set; }

    }
}
