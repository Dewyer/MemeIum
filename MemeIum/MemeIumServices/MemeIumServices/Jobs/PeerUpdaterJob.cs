using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;

namespace MemeIumServices.Jobs
{
    public class PeerUpdaterJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var ndc = JobScheduler.NodeComService;
            if (ndc != null)
            {
                ndc.UpdatePeers();
                if (ndc.Peers.Count != 0)
                {
                    ndc.OnNetworkOnline();
                }
            }

        }
    }
}
