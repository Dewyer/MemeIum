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
                tracker.Reset();
                return new ObjectResult(tracker.GetAllPeers());
            }
        }
        [Route("set")]
        public IActionResult Reg(string ip)
        {
            var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
            try
            {
                var suc = tracker.SignInPeer(remoteIpAddress.ToString(), int.Parse(ip.Split('|')[1]));
                return Ok();
            }
            catch
            {
                tracker.Reset();
            }
            return NotFound();
        }

    }
}