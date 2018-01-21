using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MemeIum.Services.EmbededWebServer.Controllers;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Swan;

namespace MemeIum.Services.EmbededWebServer
{
    class EmbededWebServer : IService,IEmbededWebServer
    {
        public bool Running { get; set; }
        public int WebServicePort { get; set; }
        private WebServer _server { get; set; }
        private ILogger _logger;
        private IMappingService _mappingService;

        public void Init()
        {
            _logger = Services.GetService<ILogger>();
            _mappingService = Services.GetService<IMappingService>();
            _logger.Log("Starting web server.");

            Running = true;
            WebServicePort = Configurations.Config.MainPort+1;
            var sTh = new Thread(new ThreadStart(RunServer));
            sTh.IsBackground = true;
            sTh.Start();
        }

        private void RunServer()
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
            var prefixes = new string[] { "http://*:" + WebServicePort+"/"};
            _server = new WebServer(prefixes, RoutingStrategy.Regex);
            //_server.Listener.Prefixes.Add("http://*:"+WebServicePort+"/");
            _server.RegisterModule(new WebApiModule());
            _server.Module<WebApiModule>().RegisterController<WebServiceController>();
            var cr = new CancellationToken();
            //_server.Listener.Start();
            _server.RunAsync(cr);
            while (Running)
            {
            }
            _server.Dispose();
        }
    }
}
