using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models.Transaction;

namespace MemeIumServices.Models
{
    public class Competition
    {
        [Key]
        public string CompetitionId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime LastPrizeUpdate { get; set; }

        public string PrizePoolJson { get; set; }
    }
}
