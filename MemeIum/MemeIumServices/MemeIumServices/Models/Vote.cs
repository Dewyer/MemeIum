using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public class Vote
    {
        [Key]
        public string VoteId { get; set; }

        public string VoterUserId { get; set; }
        public DateTime VoteAt { get; set; }
        public string VoterIpAddress { get; set; }
        public string ApplicationId { get; set; }
    }
}
