using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncCancel.Controllers
{
    public class HomeController : AsyncController
    {
	// This makes it statically private to every users so this isn't the best declaration
	// but it gets the job done in this instance
	// I'd much rather store this in the user's session state but that's a different topic
        private static Dictionary<string, CancellationTokenSource> TokenList = new Dictionary<string, CancellationTokenSource>();

        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to a sample MVC Async Example!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public void ShortAsync(int id)
        {
            var threads = GetCancelTokens();
            AsyncManager.OutstandingOperations.Increment();

            var tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;

            Task.Factory.StartNew(() =>
                {
                    var i = 0;
                    var msg = "Short {0} second call {1}";
                    var success = string.Empty;
                    while (i < id)
                    {
                        success = "Cancelled";
                        if (cancelToken.IsCancellationRequested)
                            break;
                        Thread.Sleep(new TimeSpan(0, 0, 1));
                        i++;
                        success = "Completed";
                    }
                    threads.Remove("Short" + id);
                    AsyncManager.Parameters["msg"] = string.Format(msg, id, success);
                    AsyncManager.OutstandingOperations.Decrement();
                }, cancelToken);

            threads.Add("Short" + id, tokenSource);
        }

        public JsonResult ShortCompleted(string msg)
        {
            return new JsonResult { Data = msg, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public void CancelAsync(string id)
        {
	    // access the static cancellation tokens
            var threads = GetCancelTokens();

            if (threads.ContainsKey(id))
            {
		// request the async process cancel
                threads[id].Cancel();
                AsyncManager.Parameters["msg"] = string.Format("Request {0} has been cancelled", id);
            }
            else
            {
                AsyncManager.Parameters["msg"] = string.Format("Request {0} not found, may have already completed", id);
            }
        }

        public JsonResult CancelCompleted(string msg)
        {
            return new JsonResult
            {
                Data = msg,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

	/// <summary>
	/// This is an emulation of something like a webservice
	/// or long running process that runs in its own non-blocking thread.
	/// I'm storing the object in the users session so it can be cancelled
	/// </summary>
	/// <returns></returns>
        private Dictionary<string, CancellationTokenSource> GetCancelTokens()
        {
            return TokenList;
            //var tokenList = HttpContext.Session["CancelTokens"];
            //if (tokenList == null)
            //    HttpContext.Session["CancelTokens"] = new Dictionary<string, CancellationTokenSource>();
            //return (Dictionary<string, CancellationTokenSource>)HttpContext.Session["CancelTokens"];
        }
    }
}
