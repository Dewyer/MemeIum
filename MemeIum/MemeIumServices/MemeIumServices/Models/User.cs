using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public class User
    {
        [MaxLength(50), MinLength(5)]
        public string Email { get; set; }
        public string HashedPassword { get; set; }
        [Key]
        public Guid UId { get; set; }
        public DateTime RegisteredTime { get; set; }

        public List<Wallet> Wallets { get; set; }
        public List<HistoricalTransaction> HistoricalTransactions { get; set; }
        public List<UserToken> UserTokens { get; set; }
    }
}
