using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.ViewModels
{
    public class ActiveCompetitionViewModel
    {
        public List<ApplicationStatsViewModel> Applications { get; set; }
        public List<ApplicationStatsViewModel> WinnerApplications { get; set; }
        public float TotalPrizePool { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
