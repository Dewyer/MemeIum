﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MemeIum.Misc;
using MemeIum.Requests;

namespace MemeIum.Services.CatchUp
{
    interface ICatchUpService
    {
        bool CaughtUp { get; }
        Thread Checker { get; set; }
        void ParseCatcherUpRequest(CatcherUpRequest request,Peer from);
        void ParseCatchUpData(object data);
    }
}
