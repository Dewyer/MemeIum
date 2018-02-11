using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models;

namespace MemeIumServices.ViewModels
{
    public class OverCompetitionViewModel
    {
        public Competition Competition { get; set; }
        public float TotalPrizePool { get; set; }
        public List<ApplicationStatsViewModel> Winners { get; set; }
    }
}
