using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy.Hosting.Owin.Tests.Fakes;

namespace Gate.TestHelpers
{
    
    public class FakeApp
    {
        public FakeApp()
        {
            Headers = new Dictionary<string, string>();
        }

        public FakeApp(string status, string body)
        {
            Status = status;
            Headers = new Dictionary<string, string>();
            var buffer = Encoding.UTF8.GetBytes(body);
            Body = new FakeProducer(false, buffer, 5, true);
        }

        public FakeApp(Exception faultException)
        {
            FaultException = faultException;
        }

        /// <summary>
        /// Indicates if AppDelegate method was called
        /// </summary>
        public bool AppDelegateInvoked{get;private set;}

        /// <summary>
        /// Determines the status that will be passed to result delegate by Call
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Determines the response headers that will be passed to result delegate by Call
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Determines the response body that will be passed to result delegate by Call
        /// </summary>
        public FakeProducer Body { get; set; }

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
        public Environment Owin { get {return new Environment(Env);} }

        public AppDelegate GetAppDelegate()
        {
            return new AppDelegate((
                IDictionary<string, object> env, 
                ResultDelegate result,
                Action<Exception> fault) => { AppDelegate(env, result, fault); });
        }

        /// <summary>
        /// The actual app delegate
        /// </summary>
        /// <param name="env"></param>
        /// <param name="result"></param>
        /// <param name="fault"></param>
        public void AppDelegate(
            IDictionary<string, object> env, 
            ResultDelegate result,
            Action<Exception> fault)
        {
            AppDelegateInvoked = true;
            Env = env;
            if (FaultException != null)
            {
                fault(FaultException);
            }
            else
            {
                result(Status, Headers, Body.BodyDelegate);
            }
        }
    }
}
