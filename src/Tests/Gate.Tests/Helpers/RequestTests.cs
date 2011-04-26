using System.Collections.Generic;
using Gate.Helpers;
using NUnit.Framework;

namespace Gate.Tests.Helpers
{
    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class RequestTests
    {
        [Test]
        public void QueryString_is_used_to_populate_Query_dictionary()
        {
            var env = new Dictionary<string, object>();
            new Environment(env) {QueryString = "foo=bar"};

            var request = new Request(env);
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Changing_QueryString_in_environment_reparses_Query_dictionary()
        {
            var env = new Dictionary<string, object>();
            new Environment(env) {QueryString = "foo=bar"};

            var request = new Request(env);
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));

            new Environment(env) {QueryString = "foo=quux"};
            Assert.That(request.Query["foo"], Is.EqualTo("quux"));
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
            var headers = new Dictionary<string, string> {{"Host", "Beta:8080"}};
            var env = new Dictionary<string, object>
            {
                {"server.SERVER_NAME", "Alpha"},
                {Environment.RequestHeadersKey, headers}
            };
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("Beta"));
        }

        [Test]
        public void Host_is_null_if_nothing_provided()
        {
            var env = new Dictionary<string, object> ();
            var request = new Request(env);
            Assert.That(request.Host, Is.Null);
        }
    }
}