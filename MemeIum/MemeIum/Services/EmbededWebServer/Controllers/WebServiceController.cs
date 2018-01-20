using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MemeIum.Misc.Transaction;
using MemeIum.Services.Eventmanagger;
using MemeIum.Services.Other;
using MemeIum.Services.UI;
using MemeIum.Services.Wallet;
using Newtonsoft.Json;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Swan;

namespace MemeIum.Services.EmbededWebServer.Controllers
{
    class WebServiceController : WebApiController
    {
        private ITransactionVerifier _transactionVerifier;
        private IEventManager _eventManager;

        public WebServiceController()
        {
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _eventManager = Services.GetService<IEventManager>();
        }

        [WebApiHandler(HttpVerbs.Any, "/test")]
        public bool Test(WebServer server, HttpListenerContext context)
        {
            return context.JsonResponse(new {test = true});
        }

        [WebApiHandler(HttpVerbs.Get, "/api/getbalance/{addr}")]
        public bool GetBalance(WebServer server, HttpListenerContext context, string addr)
        {
            try
            {
                Console.WriteLine("[HTTPInfo]Got asked to balance.");
                var vouts = _transactionVerifier.GetAllTransactionVOutsForAddress(addr);
                return context.JsonResponse(vouts);
            }
            catch (Exception ex)
            {
                return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<string> GetTransactionFromServices(string site,string id)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var uri = new Uri($"http://{site}/trans/{id}.json");
                    var ss = await client.GetStringAsync(uri);
                    return ss;
                }
            }
            catch (Exception e)
            {
                return "";
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/api/sendtransaction/{site}/{id}")]
        public bool PostTransactionProposal(WebServer server, HttpListenerContext context,string site,string id)
        {
            try
            {
                Console.WriteLine("[HTTPInfo]Got asked to deliver trans : {0} {1}",site,id);
                var tt = GetTransactionFromServices(site,id);
                tt.Wait();
                var trans = JsonConvert.DeserializeObject<Transaction>(tt.Result);
                if (trans != null)
                {
                    if (_transactionVerifier.Verify(trans))
                    {
                        _eventManager.PassNewTrigger(trans, EventTypes.EventType.NewTransaction);
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        return context.JsonResponse(new {ok=true});
                    }
                }
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return context.JsonResponse(new { ok = false });

            }
            catch (Exception ex)
            {
                return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
            }
        }

        protected bool HandleError(HttpListenerContext context, Exception ex, int statusCode = 500)
        {
            var errorResponse = new
            {
                Title = "Unexpected Error",
                ErrorCode = ex.GetType().Name,
                Description = ex.ExceptionMessage(),
            };

            context.Response.StatusCode = statusCode;
            return context.JsonResponse(errorResponse);
        }
    }
}
