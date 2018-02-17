using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;

namespace MemeIumServices.Jobs
{
    public class JobScheduler
    {
        public static IScheduler Scheduler;
        public static IJobDetail PrizePoolUpdater;
        public static IJobDetail EndOfCompetition;
        public static string WebAddress = "";

        public static async void Start()
        {
            Scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await Scheduler.Start();

            var job = JobBuilder.Create<PrizePoolUpdater>().Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("Update","PrizePool")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(3)
                    .RepeatForever())
                .Build();

            Scheduler.ScheduleJob(job, trigger);
            PrizePoolUpdater = job;
            //
        }
    }
}
