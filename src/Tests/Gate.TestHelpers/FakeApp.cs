using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Owin;

namespace Gate.TestHelpers
{
    public class FakeApp
    {
        public FakeApp() : this(default(string)) { }
        public FakeApp(string status) : this(status, default(string)) { }
        public FakeApp(string status, string body) : this (status, default(BodyDelegate))
        {
            if (body != null)
            {
                var buffer = Encoding.UTF8.GetBytes(body);
                Body = new FakeProducer(false, buffer, 5, true).BodyDelegate;
            }
        }

        public FakeApp(string status, BodyDelegate body)
        {
            Status = status ?? "200 OK";
            Headers = Gate.Headers.New();
            Body = body;
            AppDelegate = Call;
        }

        public FakeApp(Exception faultException)
        {
            FaultException = faultException;
            AppDelegate = Call;
        }

        /// <summary>
        /// Indicates if AppDelegate method was called
        /// </summary>
        public bool AppDelegateInvoked { get; private set; }

        /// <summary>
        /// Indicates if result or fault should be called on a different thread
        /// </summary>
        public bool SendAsync { get; set; }

        /// <summary>
        /// Determines the status that will be passed to result delegate by Call
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Determines the response headers that will be passed to result delegate by Call
        /// </summary>
        public IDictionary<string, string[]> Headers { get; set; }

        /// <summary>
        /// Determines the response body that will be passed to result delegate by Call
        /// </summary>
        public BodyDelegate Body { get; set; }

        /// <summary>
        /// If not null, this Exception is passed to fault instead of 
        /// </summary>
        public Exception FaultException { get; set; }

        /// <summary>
        /// Gets the most recent environment passed to Call
        /// </summary>
        public IDictionary<string, object> Env { get; private set; }

        /// <summary>
        /// Gets an Owin property adapter arount the most recent environment
        /// </summary>
        public Environment Owin
        {
            get { return new Environment(Env); }
        }

        public AppDelegate AppDelegate { get; set; }

        /// <summary>
        /// The actual app delegate
        /// </summary>
        /// <param name="env"></param>
        /// <param name="result"></param>
        /// <param name="fault"></param>
        public void Call(
            IDictionary<string, object> env,
            ResultDelegate result,
            Action<Exception> fault)
        {
            AppDelegateInvoked = true;
            Env = env;
            Action call = () =>
            {
                if (FaultException != null)
                {
                    fault(FaultException);
                }
                else
                {
                    result(Status, Headers, Body);
                }
            };

            if (SendAsync)
                ThreadPool.QueueUserWorkItem(_ => call());
            else
                call();
        }
    }
}