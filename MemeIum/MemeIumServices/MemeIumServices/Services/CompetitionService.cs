using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Jobs;
using MemeIumServices.Models;
using MemeIumServices.Models.Transaction;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Quartz;
using SixLabors.ImageSharp;

namespace MemeIumServices.Services
{
    public interface ICompetitionService
    {
        bool Apply(ApplicationViewModel model,User user);
        ActiveCompetitionViewModel GetActiveCompetitionViewModel(User usr);
        bool VoteForApplication(User usr, string id, string ip);
        OverCompetitionsViewModel GetOverCompetitions();
        void UpdatePrizePool();
        void EndCompetition();
        void PayUnpaidPrizes();
    }

    public class CompetitionService : ICompetitionService
    {
        private IHostingEnvironment _environment;
        private INodeComService _nodeCom;
        private MemeOffContext _context;
        private UASContext _uasContext;
        private const string ServerAddr = "RMxURU0aPNU9psNDlgfP8QtXuG0l9udPEy7tzW8M3pg=";

        public CompetitionService(IHostingEnvironment environment, MemeOffContext context, INodeComService nodeCom, UASContext uasContext)
        {
            this._environment = environment;
            this._context = context;
            _nodeCom = nodeCom;
            _uasContext = uasContext;
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
            _context.Database.EnsureCreated();
            var usrApps = _context.Applications.Where(r => r.OwnerId == user.UId).ToList();
            if (usrApps.Count > 0)
            {
                return false;
            }

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

            if (JobScheduler.EndOfCompetition != null)
            {
                var newJob = JobBuilder.Create<EndOfCompetitionJob>();

            }
            return comp;
        }

        public void SaveUnpaidPrize(Transaction trans)
        {
            var path = $"{_environment.ContentRootPath}\\MemeOff\\unpaidPrizes.json";
            if (!File.Exists(path))
            {
                File.WriteAllText(path,"[]");
            }

            var content = File.ReadAllText(path);
            var oldContent = JsonConvert.DeserializeObject<List<Transaction>>(content);
            oldContent.Add(trans);
            var newContent = JsonConvert.SerializeObject(oldContent);
            File.WriteAllText(path,newContent);
        }



        public void EndCompetition()
        {
            var overs = _context.Competitions.Where(r=>r.EndTime<= DateTime.UtcNow).OrderBy(r=>r.EndTime).ToList();
            overs.Reverse();
            if (overs.Count == 0)
            {
                return;
            }

            var latest = overs[0];
            if ((DateTime.UtcNow - latest.EndTime).TotalSeconds <= 10)
            {
                var prizes = JsonConvert.DeserializeObject<List<PrizeOffer>>(latest.PrizePoolJson);
                _nodeCom.UpdatePeers();
                var unspent = _nodeCom.GetUnspentVOutsForAddress(ServerAddr);

                if (prizes.TrueForAll(r => unspent.Exists(x => x.Id == r.VoutId)))
                {
                    var winners = GetCompetitionWinners(latest.CompetitionId);
                    var winnerApps = winners.Select(winner => _context.Applications.First(r => r.ApplicationId == winner.ApplicationId)).ToList();

                    if (winnerApps.Count == 0)
                    {
                        return;
                    }

                    var totalPrize = prizes.Sum(r => r.Amount);
                    var winnerShare = (long)((totalPrize / winnerApps.Count)*100000L);

                    var desired = new List<InBlockTransactionVOut>();
                    foreach (var application in winnerApps)
                    {
                        var vo = new TransactionVOut()
                        {
                            Amount = winnerShare,
                            FromAddress = ServerAddr,
                            ToAddress = application.RewardWallet
                        };
                        TransactionVOut.SetUniqueIdForVOut(vo);
                        desired.Add(vo.GetInBlockTransactionVOut());
                    }
                    var wallet = _uasContext.Wallets.First(r=>r.Address == ServerAddr);
                    var suc = _nodeCom.SendTransactionFromData(wallet,"Winners of MemeOff",desired,unspent,out Transaction transaction);
                    if (!suc)
                    {
                        Task.Run(() => SaveUnpaidPrize(transaction));
                    }

                }
            }
        }

        public void PayUnpaidPrizes()
        {
            var path = $"{_environment.ContentRootPath}\\MemeOff\\unpaidPrizes.json";
            if (!File.Exists(path))
            {
                return;
            }

            var prizes = JsonConvert.DeserializeObject<List<Transaction>>(File.ReadAllText(path));
            File.WriteAllText(path,"[]");
            if (prizes.Count > 0)
            {
                var wallet = _uasContext.Wallets.First(r => r.Address == ServerAddr);
                var desired = new List<InBlockTransactionVOut>();
                var unspent = _nodeCom.GetUnspentVOutsForAddress(ServerAddr);

                var serverBal = unspent.Sum(r => r.Amount);

                foreach (var tran in prizes)
                {
                    desired.AddRange(tran.Body.VOuts);
                }
                var prizeBal = desired.Sum(r => r.Amount);

                var suc = _nodeCom.SendTransactionFromData(wallet, "Winners of MemeOff", desired, unspent, out Transaction transaction);
                if (!suc)
                {
                    Task.Run(() => SaveUnpaidPrize(transaction));
                }

            }

            
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

        public string GetDonationUrl(string contest)
        {
            var qr = QueryHelpers.AddQueryString("/Home/PayRequest", "msgp",
                "Thansk for your donation!");
            qr = QueryHelpers.AddQueryString(qr, "msgt", $"memeoffdonation:{contest}");
            qr = QueryHelpers.AddQueryString(qr, "amm", "1");
            qr = QueryHelpers.AddQueryString(qr, "addr", ServerAddr);
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
                TotalPrizePool = prizePool.Sum(r=>r.Amount),
                DonateUrl = GetDonationUrl(ac.CompetitionId)
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

        public void UpdatePrizePool()
        {
            var curComp = GetActiveCompetitionOrCreateNewCompetition();

            if ((DateTime.UtcNow - curComp.LastPrizeUpdate).TotalMinutes >= 2)
            {
                _nodeCom.UpdatePeers();
                var unspent = _nodeCom.GetUnspentVOutsForAddress(ServerAddr);
                if (unspent == null)
                {
                    unspent = new List<TransactionVOut>();
                }
                var prizePool = JsonConvert.DeserializeObject<List<PrizeOffer>>(curComp.PrizePoolJson);
                var newPool = new List<PrizeOffer>();
                newPool.AddRange(prizePool);

                foreach (var transactionVOut in unspent)
                {
                    if (prizePool.FindAll(r => r.VoutId == transactionVOut.Id).Count == 0)
                    {
                        var msg = _nodeCom.GetMessageTransactionMessageFromVoutId(transactionVOut.Id);
                        msg = msg.ToLower();

                        if (msg.StartsWith("memeoffdonation:"))
                        {
                            var tokens = msg.Split(':');
                            if (tokens.Length == 2)
                            {
                                var compId = tokens[1];
                                if (compId == curComp.CompetitionId)
                                {
                                    var prizeOffer = new PrizeOffer(){Address = transactionVOut.FromAddress,Amount = (float)(transactionVOut.Amount/100000L),VoutId = transactionVOut.Id};
                                    newPool.Add(prizeOffer);
                                }
                            }
                        }
                    }
                }

                var newPoolJson = JsonConvert.SerializeObject(newPool);
                curComp.PrizePoolJson = newPoolJson;
                curComp.LastPrizeUpdate = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }
    }
}
