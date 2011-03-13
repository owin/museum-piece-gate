using System;
using System.Collections.Generic;
using Nancy;
using NUnit.Framework;

namespace Gate.Nancy.Tests
{
    [TestFixture]
    public class ApplicationTests
    {
        Request _request;
        StubEngine _engine;
        Func<Request, NancyContext> _handleRequest;
        Application _application;
        IDictionary<string, object> _env;

        [SetUp]
        public void Init()
        {
            _request = null;
            _handleRequest = request => new NancyContext
            {
                Request = request,
                Response = new Response
                {
                    StatusCode = HttpStatusCode.OK,
                    Headers = new Dictionary<string, string>(),
                    Contents = stream => { },
                },
            };
            _engine = new StubEngine(r =>
            {
                _request = r;
                return _handleRequest(_request);
            });
            _application = new Application(_engine);

            _env = new Dictionary<string, object>();
            new Environment(_env)
            {
                Version = "1.0",
                Method = "GET",
                UriScheme = "http",
                ServerName = "localhost",
                ServerPort = "80",
                BaseUri = "",
                RequestUri = "/",
                Headers = new Dictionary<string, string>(),
                Body = (next, error, complete) =>
                {
                    complete();
                    return () => { };
                },
            };
        }

        void Execute()
        {
            _application.Call(_env, ex => { throw ex; }, (status, headers, body) => { });
        }

        [Test]
        public void Handle_request_is_called_with_the_env()
        {
            Execute();

            Assert.That(_request, Is.Not.Null);
        }

        [Test]
        public void Query_string_is_split_away_from_requesturi()
        {
            new Environment(_env).RequestUri = "/alpha?beta=gamma";

            Execute();

            Assert.That((string) _request.Query.beta, Is.EqualTo("gamma"));
        }
    }
}
