using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public class UserToken
    {
        [Key]
        public string Token { get; set; }
        public DateTime Expiration { get; set; }

        public string OwnerId { get; set; }

        [ForeignKey("UserForeignKey")]
        public User User { get; set; }
    }
}
