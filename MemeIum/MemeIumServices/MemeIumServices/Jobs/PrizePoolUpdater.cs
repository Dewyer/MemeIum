using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Quartz;

namespace MemeIumServices.Jobs
{
    public class PrizePoolUpdater : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            using (var client = new HttpClient())
            {
                var ss = await client.GetStringAsync(new Uri($"{JobScheduler.WebAddress}/MemeOff/PrizePoolUpdate"));
            }
        }
    }
}
