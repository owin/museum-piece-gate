using System.Collections.Generic;
using Gate.Helpers;
using NUnit.Framework;

namespace Gate.Helpers.Tests
{
    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class RequestTests
    {
        [Test]
        public void QueryString_is_used_to_populate_Query_dictionary()
        {
            var request = new Request(new Environment()) { QueryString = "foo=bar" };
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Changing_QueryString_in_environment_reparses_Query_dictionary()
        {
            var request = new Request(new Environment()) { QueryString = "foo=bar" };
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));

            request.QueryString = "foo=quux";
            Assert.That(request.Query["foo"], Is.EqualTo("quux"));
        }

        [Test]
        public void Body_is_used_to_populate_Post_dictionary()
        {
            var request = new Request(new Environment()) { Method = "POST", Body = Body.FromText("foo=bar") };
            Assert.That(request.Post["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Changing_Body_in_environment_reparses_Post_dictionary()
        {
            var request = new Request(new Environment()) { Method = "POST", Body = Body.FromText("foo=bar") };
            Assert.That(request.Post["foo"], Is.EqualTo("bar"));

            request.Body = Body.FromText("foo=quux");
            Assert.That(request.Post["foo"], Is.EqualTo("quux"));
        }

        [Test]
        public void Host_will_use_cgi_SERVER_NAME_if_present()
        {
            var env = new Dictionary<string, object> {{"server.SERVER_NAME", "Alpha"}};
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("Alpha"));
        }

        [Test]
        public void Host_should_favor_Host_header_if_present()
        {
            var headers = new Dictionary<string, string> {{"Host", "Beta"}};
            var env = new Dictionary<string, object>
            {
                {"server.SERVER_NAME", "Alpha"},
                {Environment.RequestHeadersKey, headers}
            };
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("Beta"));
        }

        [Test]
        public void Host_will_remove_port_from_request_header_if_needed()
        {
            var env = new Dictionary<string, object>
            {
                {"server.SERVER_NAME", "Alpha"},
                {"owin.RequestHeaders", new Dictionary<string, string> {{"Host", "Beta:8080"}}}
            };
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("Beta"));
        }

        [Test]
        public void Host_is_null_if_nothing_provided()
        {
            var env = new Dictionary<string, object>();
            var request = new Request(env);
            Assert.That(request.Host, Is.Null);
        }

        [Test]
        public void ContentType_and_MediaType_should_return_http_header()
        {
            var env = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string> {{"Content-Type", "text/plain"}}}
            };
            var request = new Request(env);
            Assert.That(request.ContentType, Is.EqualTo("text/plain"));
            Assert.That(request.MediaType, Is.EqualTo("text/plain"));
        }

        [Test]
        public void MediaType_is_shorter_when_delimited()
        {
            var env = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string> {{"Content-Type", "text/html; charset=utf-8"}}}
            };
            var request = new Request(env);
            Assert.That(request.ContentType, Is.EqualTo("text/html; charset=utf-8"));
            Assert.That(request.MediaType, Is.EqualTo("text/html"));
        }

        [Test]
        public void ContentType_and_MediaType_are_null_when_missing()
        {
            var env = new Dictionary<string, object>
            {
                {"owin.RequestHeaders", new Dictionary<string, string>()}
            };
            var request = new Request(env);
            Assert.That(request.ContentType, Is.Null);
            Assert.That(request.MediaType, Is.Null);
        }
    }
}