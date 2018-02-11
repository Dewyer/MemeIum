using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Models;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using SixLabors.ImageSharp;

namespace MemeIumServices.Services
{
    public interface ICompetitionService
    {
        bool Apply(ApplicationViewModel model,User user);
        ActiveCompetitionViewModel GetActiveCompetitionViewModel(User usr);
        bool VoteForApplication(User usr, string id, string ip);
        OverCompetitionsViewModel GetOverCompetitions();
    }

    public class CompetitionService : ICompetitionService
    {
        private IHostingEnvironment _environment;
        private MemeOffContext _context;

        public CompetitionService(IHostingEnvironment environment, MemeOffContext context)
        {
            this._environment = environment;
            this._context = context;
        }

        public bool IsFileOkay(string fname,out Image<Rgba32> img)
        {
            try
            {
                var ii = Image.Load(File.ReadAllBytes(fname));

                if (ii.Height <= 3000 && ii.Width <= 3000)
                {
                    img = ii;
                    return true;
                }
            }
            catch
            {
            }
            img = null;
            return false;
        }

        public bool IsApplicationTitleAndWalletOkay(ApplicationViewModel model)
        {
            var title= !String.IsNullOrWhiteSpace(model.Title) && model.Title.Length <= 30;
            var wallet = false;
            var wTokens = model.Wallet.Split('-');
            if (wTokens.Length > 1)
            {
                wallet = wTokens[1].Length <= 100 && !String.IsNullOrWhiteSpace(wTokens[1]);
            }
            return title && wallet;
        }

        public bool Apply(ApplicationViewModel model, User user)
        {
            try
            {
                var id = Guid.NewGuid().ToString();
                var startName = String.Join("", ContentDispositionHeaderValue
                    .Parse(model.Image.ContentDisposition)
                    .FileName
                    .Trim().ToString().Split('"'));
                var ext = Path.GetExtension(startName);
                var filename = $"{_environment.WebRootPath}\\MemeOff\\{id}{ext}";
                using (FileStream fs = System.IO.File.Create(filename))
                {
                    model.Image.CopyTo(fs);
                    fs.Flush();
                }

                if (IsFileOkay(filename, out Image<Rgba32> img) && IsApplicationTitleAndWalletOkay(model))
                {
                    var ac = GetActiveCompetitionOrCreateNewCompetition();
                    var app = new Application()
                    {
                        ApplicationId = id,
                        ApplicationTime = DateTime.UtcNow,
                        ImagePath = $"{id}{ext}",
                        ImageHeight = img.Height,
                        ImageWidth = img.Width,
                        OwnerId = user.UId,
                        RewardWallet = model.Wallet.Split('-')[1],
                        CompetitionId = ac.CompetitionId,
                        Title = model.Title
                    };
                    _context.Database.EnsureCreated();
                    _context.Applications.Add(app);
                    _context.SaveChanges();
                    return true;
                }
                else
                {
                    File.Delete(filename);
                }
            }
            catch
            {
            }
            return false;
        }

        public Competition CreateNewCompetition()
        {
            var prizePool = new List<PrizeOffer>() { };

            var prizePoolJson = JsonConvert.SerializeObject(prizePool);
            var id = Guid.NewGuid().ToString();
            var comp = new Competition()
            {
                CompetitionId = id,
                EndTime = DateTime.UtcNow.AddDays(1),
                StartTime = DateTime.UtcNow,
                PrizePoolJson = prizePoolJson,
                LastPrizeUpdate = DateTime.UtcNow
            };

            _context.Database.EnsureCreated();
            _context.Competitions.Add(comp);
            _context.SaveChanges();
            return comp;
        }

        public Competition GetActiveCompetitionOrCreateNewCompetition()
        {
            var ac = _context.Competitions.Where(r => r.StartTime < DateTime.UtcNow && r.EndTime >= DateTime.UtcNow).OrderBy(r=>r.StartTime).ToList();
            if (ac.Count == 0)
            {
                var comp = CreateNewCompetition();
                return comp;
            }
            return ac[0];
        }

        public string GetTipUrl(string to)
        {
            var qr = QueryHelpers.AddQueryString("/Home/PayRequest", "msgp",
                "Thansk for your tip!");
            qr = QueryHelpers.AddQueryString(qr, "msgt", "MemeOff Tip");
            qr = QueryHelpers.AddQueryString(qr, "amm", "1");
            qr = QueryHelpers.AddQueryString(qr, "addr", to);
            return qr;
        }

        public ApplicationStatsViewModel GetStatsFromApplication(Application app,string competition,User usr)
        {
            var tipurl = GetTipUrl(app.RewardWallet);
            var votes = _context.Votes.Where(r => r.ApplicationId == app.ApplicationId).ToList().Count;

            var voteState = VoteState.NotVoted;
            if (usr != null)
            {
                var vote = _context.Votes.Where(r => r.VoterUserId == usr.UId && r.CompetitionId == competition).ToList();
                if (vote.Count == 1)
                {
                    voteState = vote[0].ApplicationId == app.ApplicationId ? VoteState.VotedForThis : VoteState.NotVotedForThis;
                }
            }

            var stat = new ApplicationStatsViewModel()
            {
                ImageName = app.ImagePath,
                ImageHeight = app.ImageHeight,
                ImageWidth = app.ImageWidth,
                TipUrl = tipurl,
                Votes = votes,
                Title = app.Title,
                CreatedAt = app.ApplicationTime,
                VoteState = voteState,
                ApplicationId = app.ApplicationId
            };
            return stat;
        }

        public ActiveCompetitionViewModel GetActiveCompetitionViewModel(User usr)
        {
            _context.Database.EnsureCreated();
            var ac = GetActiveCompetitionOrCreateNewCompetition();
            var apps = _context.Applications.Where(r => r.CompetitionId == ac.CompetitionId).ToList();
            var allstats = new List<ApplicationStatsViewModel>();

            foreach (var application in apps)
            {
                allstats.Add(GetStatsFromApplication(application,ac.CompetitionId,usr));
            }
            allstats = allstats.OrderBy(r => r.Votes).ToList();
            allstats.Reverse();
            for (int ii = 0; ii < allstats.Count; ii++)
            {
                allstats[ii].Placement = ii+1;
            }
            var winners = new List<ApplicationStatsViewModel>();
            if (allstats.Count != 0)
            {
                var maxVotes = allstats.Max(r => r.Votes);
                winners = allstats.FindAll(r => r.Votes == maxVotes);
            }

            var stats = new List<ApplicationStatsViewModel>();
            foreach (var stat in allstats)
            {
                if (!winners.Contains(stat))
                {
                    stats.Add(stat);
                }
            }

            var prizePool = JsonConvert.DeserializeObject<List<PrizeOffer>>(ac.PrizePoolJson);

            var vm = new ActiveCompetitionViewModel()
            {
                Applications = stats,
                WinnerApplications = winners,
                EndTime = ac.EndTime,
                StartTime = ac.StartTime,
                TotalPrizePool = prizePool.Sum(r=>r.Amount)
            };

            return vm;
        }

        public bool VoteForApplication(User usr,string id,string ip)
        {
            var ac = GetActiveCompetitionOrCreateNewCompetition();
            if (_context.Applications.FirstOrDefault(r => r.ApplicationId == id) != null)
            {
                //var ipVotes = _context.Votes.Where(r => r.VoterIpAddress == ip).ToList();
                var votes = _context.Votes.Where(r => r.CompetitionId == ac.CompetitionId && r.VoterUserId == usr.UId).ToList();
                var newVote = new Vote
                {
                    ApplicationId = id,
                    CompetitionId = ac.CompetitionId,
                    VoteAt = DateTime.UtcNow,
                    VoteId = Guid.NewGuid().ToString(),
                    VoterIpAddress = ip,
                    VoterUserId = usr.UId
                };
                _context.Database.EnsureCreated();
                
                if (votes.Count == 0)
                {
                    _context.Votes.Add(newVote);
                    _context.SaveChanges();
                    return true;
                }
                else if (votes[0].ApplicationId != id)
                {
                    _context.Votes.Add(newVote);
                    _context.Votes.Remove(votes[0]);
                    _context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        public List<ApplicationStatsViewModel> GetCompetitionWinners(string compId)
        {
            var apps = _context.Applications.Where(r => r.CompetitionId == compId).ToList();
            if (apps.Count != 0)
            {
                var allStats = new List<ApplicationStatsViewModel>();
                foreach (var application in apps)
                {
                    var stat = GetStatsFromApplication(application, compId, null);
                    stat.Placement = 0;
                    allStats.Add(stat);
                }

                var maxV = allStats.Max(r => r.Votes);
                var winners = allStats.FindAll(r => r.Votes == maxV);
                return winners;
            }
            else
            {
                return new List<ApplicationStatsViewModel>();
            }
        }

        public OverCompetitionsViewModel GetOverCompetitions()
        {
            _context.Database.EnsureCreated();
            var lifeTime = 0f;
            var overs = new List<OverCompetitionViewModel>();
            var allOver = _context.Competitions.Where(r => r.EndTime <= DateTime.UtcNow).ToList();

            foreach (var competition in allOver)
            {
                var pp = JsonConvert.DeserializeObject<List<PrizeOffer>>(competition.PrizePoolJson);
                var pPoolAm = pp.Sum(r => r.Amount);
                lifeTime += pPoolAm;
                var over = new OverCompetitionViewModel()
                {
                    Competition = competition,
                    TotalPrizePool = pPoolAm,
                    Winners = GetCompetitionWinners(competition.CompetitionId)
                };
                overs.Add(over);
            }
            return new OverCompetitionsViewModel()
            {
                LifeTimePrizes = lifeTime,
                OverCompetitions = overs
            };
        }
    }
}
