using System;
using System.Linq;
using System.Text;
using Nancy;

namespace Gate.Nancy.Tests
{
    public class StubEngine : INancyEngine
    {
        readonly Func<Request, NancyContext> _handleRequest;

        public StubEngine(Func<Request, NancyContext> handleRequest)
        {
            _handleRequest = handleRequest;
        }

        public NancyContext HandleRequest(Request request)
        {
            return _handleRequest(request);
        }

        public Func<NancyContext, Response> PreRequestHook { get; set; }

        public Action<NancyContext> PostRequestHook { get; set; }
    }
}