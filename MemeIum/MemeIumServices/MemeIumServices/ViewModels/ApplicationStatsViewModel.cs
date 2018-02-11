using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemeIumServices.ViewModels
{
    public enum VoteState
    {
        VotedForThis,
        NotVotedForThis,
        NotVoted
    }

    public class ApplicationStatsViewModel
    {
        public string ImageName { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }


        public string TipUrl { get; set; }
        public int Votes { get; set; }
        public int Placement { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public VoteState VoteState { get; set; }
        public string ApplicationId { get; set; }

    }
}
