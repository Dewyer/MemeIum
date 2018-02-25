using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Services;
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

        public static INodeComService NodeComService;

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

            var peerUpdate = JobBuilder.Create<PeerUpdaterJob>().Build();
            ITrigger peerTrigger = TriggerBuilder.Create()
                .WithIdentity("PeerUpdate", "PeerUpdate")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();

            Scheduler.ScheduleJob(job, trigger);
            Scheduler.ScheduleJob(peerUpdate, peerTrigger);
            PrizePoolUpdater = job;
            //
        }
    }
}
