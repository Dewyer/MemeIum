using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
                var vouts = _transactionVerifier.GetAllTransactionVOutsForAddress(addr);
                var balance = vouts.Sum(r => r.Amount) / 100000f;

                return context.JsonResponse(new {balance=balance,Address=addr});
            }
            catch (Exception ex)
            {
                return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/api/sendtransaction/{bodyjson}")]
        public bool PostTransactionProposal(WebServer server, HttpListenerContext context, string bodyjson)
        {
            try
            {
                var trans = JsonConvert.DeserializeObject<Transaction>(bodyjson);
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
