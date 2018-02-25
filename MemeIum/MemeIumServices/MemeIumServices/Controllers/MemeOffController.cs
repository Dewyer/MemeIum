using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Services;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumServices.Controllers
{
    public class MemeOffController : Controller
    {
        private ICompetitionService _competitionService;
        private IAuthService _authService;
        private MemeOffContext _context;

        public MemeOffController(ICompetitionService competitionService, IAuthService authService, MemeOffContext context)
        {
            _competitionService = competitionService;
            _authService = authService;
            this._context = context;
        }

        public IActionResult Index()
        {
            var usr = _authService.GetAuthUser(Request, Response);
            var model = _competitionService.GetActiveCompetitionViewModel(usr);
            return View(model);
        }

        public IActionResult Application()
        {
            var usr = _authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return _authService.UnAuthenticatedResult;
            }
            
            return View(usr);
        }

        public IActionResult PrizePoolUpdate()
        {
            _competitionService.UpdatePrizePool();
            return Ok();
        }

        public IActionResult OverCompetitions()
        {
            var model = _competitionService.GetOverCompetitions();
            return View(model);
        }

        public IActionResult EndCompetition()
        {
            _competitionService.EndCompetition();
            return Ok();
        }

        public IActionResult PayUnpaid()
        {
            _competitionService.PayUnpaidPrizes();
            return Ok();
        }

        public IActionResult Vote(string app)
        {
            var usr = _authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return _authService.UnAuthenticatedResult;
            }

            var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            var vote = _competitionService.VoteForApplication(usr, app, remoteIpAddress);

            var suc = new ActionResultViewModel();
            if (vote)
            {
                suc.Title = "Success !";
                suc.SubTitle = "Vote sent successfully!";
                suc.TextClass = "text-success";
                suc.ButtonClass = "btn-success";
                suc.ActionButtonLink = "/MemeOff/Index";
                suc.ActionButtonText = "Back to the contest";

            }
            else
            {
                suc.Title = "Error !";
                suc.SubTitle = "Voting failed please try again!";
                suc.TextClass = "text-danger";
                suc.ButtonClass = "btn-danger";
                suc.ActionButtonLink = "/MemeOff/Application";
                suc.ActionButtonText = "Back to contest";

            }
            return new RedirectToActionResult("Result", "Home", routeValues: suc);

        }

        [HttpPost]
        public IActionResult DoApplication(ApplicationViewModel model)
        {
            var usr = _authService.GetAuthUser(Request, Response);
            if (usr == null)
            {
                return _authService.UnAuthenticatedResult;
            }
            var suc = new ActionResultViewModel
            {
                Title = "Error !",
                SubTitle =
                    "Application was not sent successfully! Please check your image, also your title should be less then 30 characters.",
                TextClass = "text-danger",
                ButtonClass = "btn-danger",
                ActionButtonLink = "/MemeOff/Application",
                ActionButtonText = "Back to application"
            };

            if (String.IsNullOrWhiteSpace(model.Title) || model.Image == null)
            {
                return new RedirectToActionResult("Result", "Home", routeValues: suc);
            }

            var ok = _competitionService.Apply(model, usr);
            if (ok)
            {
                suc.Title = "Success !";
                suc.SubTitle = "Application sent successfully!";
                suc.TextClass = "text-success";
                suc.ButtonClass = "btn-success";
                suc.ActionButtonLink = "/MemeOff/Index";
                suc.ActionButtonText = "Back to the contest";

            }
            return new RedirectToActionResult("Result", "Home", routeValues: suc);
        }
    }
}
