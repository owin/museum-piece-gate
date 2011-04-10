using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Gate.TestHelpers
{
    using AppDelegate = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // error

    public class AppUtils
    {
        public static CallResult Call(AppDelegate app)
        {
            var env = new Dictionary<string, object>();
            new Environment(env) {Version = "1.0"};
            var wait = new ManualResetEvent(false);
            var callResult = new CallResult();
            app(
                env,
                (status, headers, body) =>
                {
                    callResult.Status = status;
                    callResult.Headers = headers;
                    callResult.Body = body;
                    wait.Set();
                },
                exception =>
                {
                    callResult.Exception = exception;
                    wait.Set();
                });
            wait.WaitOne();
            return callResult;
        }
    }

    public class CallResult
    {
        public string Status { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> Body { get; set; }
        public Exception Exception { get; set; }
    }
}