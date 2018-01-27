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
        public string Name { get; set; }

        [Key]
        public string Address { get; set; }

        public string OwnerId { get; set; }

        [ForeignKey("UserForeignKey")]
        public User User { get; set; }

    }
}
