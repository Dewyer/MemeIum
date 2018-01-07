﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using MemeIum.Misc.Transaction;

namespace MemeIum.Services.Mineing
{
    interface IMinerService
    {
        ObservableCollection<Transaction> MemPool { get; set; }
        List<Thread> CurrentWorkers { get; set; }
        void TryRestartingWorkers();
    }
}
