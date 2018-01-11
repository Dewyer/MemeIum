﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;

namespace MemeIum.Services
{
    interface IMappingService
    {
        void InitiateSweap(List<Peer> originPeers);
        void ParseGetAddressesRequest(GetAddressesRequest request,Peer source);
        void ParseAddressesRequest(AddressesRequest request, Peer source);
        void Broadcast(object data);
        ObservableCollection<Peer> Peers { get; set; }
    }
}
