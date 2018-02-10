using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.Models
{
    public class Application
    {
        [Key]
        public string ApplicationId { get; set; }

        public string OwnerId { get; set; }
        public string ImagePath { get; set; }
        public DateTime ApplicationTime { get; set; }
    }
}
