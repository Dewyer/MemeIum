﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MemeIum.Requests;
using Newtonsoft.Json;

namespace MemeIum.Services
{
    class MockClient
    {

        public MockClient()
        {

        }

        public void MockTest1()
        {
            Console.WriteLine("Running test1");
            var target = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Configurations.Config.MainPort);
            var socket = new UdpClient();

            for (int num = 1; num <= 3; num++)
            {
                var msg = JsonConvert.SerializeObject(new MappingRequest() {Version = "0.1", Type = 0,Ask=true});

                byte[] message = Encoding.UTF8.GetBytes(msg);
                socket.Send(message, message.Length, target);
            }
        }
    }
}
