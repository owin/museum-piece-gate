using System;
using System.Collections.Generic;
using System.Text;
using Gate.Builder;
using Gate.TestHelpers;
using NUnit.Framework;
using Owin;

namespace Gate.Middleware.Tests
{
    public class BasicAuthTests
    {
        private FakeHost _host;

        [SetUp]
        public void Setup()
        {
            var app = new FakeApp("200 OK");

            Action<IAppBuilder> pipeline = b => b
                .UseBasicAuth("Howdy", (username, password) => password == "Howdy")
                .Run(app.AppDelegate);

            var config = AppBuilder.BuildConfiguration(pipeline);
            _host = new FakeHost(config);
        }

        [Test]
        public void Basic_auth_presents_challenge_when_no_credentials_are_provided()
        {
            Request(AssertBasicAuthChallenge);
        }

        [Test]
        public void Basic_auth_rechallenges_with_incorrect_credentials()
        {
            RequestWithBasicAuth("joe", "password", AssertBasicAuthChallenge);
        }

        [Test]
        public void Basic_auth_returns_ok_if_credentials_are_valid()
        {
            RequestWithBasicAuth("Boy", "Howdy", response => Assert.That(response.Status, Is.EqualTo("200 OK")));
        }

        [Test]
        public void Basic_auth_returns_bad_request_when_different_auth_scheme_is_used()
        {
            Request(new Dictionary<string, IEnumerable<string>> {{"Authorization", new[] {"Digest blah"}}}, 
                response => Assert.That(response.Status, Is.EqualTo("400 Bad Request")))
            ;
        }

        private void AssertBasicAuthChallenge(FakeHostResponse response)
        {
            Assert.That(response.Status, Is.EqualTo("401 Unauthorized"));
            Assert.That(response.Headers.Keys, Contains.Item("WWW-Authenticate"));
            Assert.That(response.Headers["WWW-Authenticate"], Contains.Item("Basic realm=\"Howdy\""));
        }

        private void RequestWithBasicAuth(string username, string password, Action<FakeHostResponse> responseAction = null)
        {
            var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));

            Request(new Dictionary<string, IEnumerable<string>> { { "Authorization", new[] { "Basic " + base64 } } }, responseAction);
        }

        private void Request(Action<FakeHostResponse> responseAction)
        {
            Request(new Dictionary<string, IEnumerable<string>>(), responseAction);
        }

        private void Request(Dictionary<string, IEnumerable<string>> headers, Action<FakeHostResponse> responseAction = null)
        {
            if (responseAction != null)
            {
                responseAction(_host.GET("/", request => request.Headers = headers));
            }
        }
    }
}