using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public class UserToken
    {
        [Key]
        public string Token { get; set; }
        public DateTime Expiration { get; set; }

        public User User { get; set; }
    }
}
