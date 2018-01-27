using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public enum TransactionState
    {
        Verified,NonVerified,Invalid
    }

    public class HistoricalTransaction
    {
        public string TransactionJson { get; set; }
        [Key]
        public string TransactionId { get; set; }
        public DateTime TimeOfCreation { get; set; }
        public TransactionState State { get; set; }

        [ForeignKey("UserForeignKey")]
        public User User { get; set; }
    }
}
