using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace MemeIumTracker.Controllers
{
    [Route("track")]
    public class TrackerController : Controller
    {
        private readonly ITrackerService tracker;

        public TrackerController(ITrackerService _tracker)
        {
            tracker = _tracker;
        }
        [Route("")]
        public IActionResult Index()
        {
            try
            {
                var peers = tracker.GetAllPeers();
                return new ObjectResult(peers);

            }
            catch (Exception e)
            {
                return new ObjectResult(e);
            }
        }
        [Route("set")]
        public IActionResult Reg(string ip)
        {
            var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;

            if (remoteIpAddress.ToString() == ip.Split('|')[0] || ip.ToList().Contains(':'))
            {
                var suc = tracker.SignInPeer(ip);
                if (!suc)
                {
                    return NotFound();
                }
                return Ok();
            }
            return new ObjectResult(new { succsess = false,ip=remoteIpAddress.ToString(),given=ip });
        }

    }
}