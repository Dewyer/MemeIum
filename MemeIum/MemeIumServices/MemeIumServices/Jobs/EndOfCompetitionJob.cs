using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Quartz;

namespace MemeIumServices.Jobs
{
    public class EndOfCompetitionJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Delay(2000);
            using (var client = new HttpClient())
            {
                var ss = await client.GetStringAsync(new Uri($"{JobScheduler.WebAddress}/MemeOff/EndCompetition"));
            }
        }
    }
}
