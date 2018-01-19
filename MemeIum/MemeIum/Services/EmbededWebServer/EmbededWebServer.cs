using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MemeIum.Services.EmbededWebServer.Controllers;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace MemeIum.Services.EmbededWebServer
{
    class EmbededWebServer : IService,IEmbededWebServer
    {
        public bool Running { get; set; }
        public int WebServicePort { get; set; }
        private WebServer _server { get; set; }
        private ILogger _logger;

        public void Init()
        {
            _logger = Services.GetService<ILogger>();
            _logger.Log("Starting web server.");

            Running = true;
            WebServicePort = Configurations.Config.MainPort + 1;
            var sTh = new Thread(new ThreadStart(RunServer));
            sTh.IsBackground = true;
            sTh.Start();
        }

        private void RunServer()
        {
            _server = new WebServer("http://localhost:"+WebServicePort+"/", RoutingStrategy.Regex);
            _server.RegisterModule(new WebApiModule());
            _server.Module<WebApiModule>().RegisterController<WebServiceController>();
            _server.RunAsync();
            while (Running)
            {
            }
            _server.Dispose();
        }
    }
}
