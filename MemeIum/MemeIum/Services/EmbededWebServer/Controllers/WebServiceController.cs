using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MemeIum.Misc.Transaction;
using MemeIum.Services.Blockchain;
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
        private IBlockChainService _blockChainService;
        private IEventManager _eventManager;

        public WebServiceController()
        {
            _transactionVerifier = Services.GetService<ITransactionVerifier>();
            _eventManager = Services.GetService<IEventManager>();
            _blockChainService = Services.GetService<IBlockChainService>();
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
                Console.WriteLine("[HTTPInfo]Got asked to balance {0}.",addr);
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
                    var uri = new Uri($"{site}/trans/{id}.json");
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
                        context.Response.StatusCode = (int) HttpStatusCode.OK;
                        Console.WriteLine("[HTTPInfo]Asked transaction accepted.");
                        return context.JsonResponse(new {ok = true});
                    }
                    else
                    {
                        Console.WriteLine("[HTTPInfo]Bad transaction");
                    }
                }
                Console.WriteLine("[HTTPInfo]Asked transaction rejected.");
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                return context.JsonResponse(new {ok=false});

            }
            catch (Exception ex)
            {
                return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/api/gettransmessage/{id}")]
        public bool GetTransactionMessage(WebServer server, HttpListenerContext context, string id)
        {
            try
            {
                Console.WriteLine("[HTTPInfo]Got asked to Transaction message {0}",id);
                var vout = _transactionVerifier.GetUnspeTransactionVOut(id, out bool spent);
                if (vout == null)
                {
                    return context.JsonResponse(new {message = ""});
                }

                var block = _blockChainService.LookUpBlock(vout.FromBlock);
                var tt = block.Body.Tx.FindAll(r => r.Body.VOuts.FindAll(l=>l.Id == id).Count > 0)[0];
                return context.JsonResponse(new {message = tt.Body.Message});
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
