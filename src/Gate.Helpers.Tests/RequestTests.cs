using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;

namespace Gate.Helpers.Tests
{
    using BodyDelegate = Func<
        // on next
        Func<
            ArraySegment<byte>, // data
            Action, // continuation
            bool // continuation was or will be invoked
            >,
        // on error
        Action<Exception>,
        // on complete
        Action,
        // cancel 
        Action
        >;

    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class RequestTests
    {
        [Test]
        public void Version_property_provide_access_to_environment()
        {
            var env = new Dictionary<string, object> {{"owin.Version", "1.0"}};
            var request = new Request(env);
            Assert.That(request.Version, Is.EqualTo("1.0"));
        }

        [Test]
        public void Envoronment_access_is_not_buffered_or_cached()
        {
            var env = new Dictionary<string, object> {{"owin.Version", "1.0"}};
            var request = new Request(env);
            Assert.That(request.Version, Is.EqualTo("1.0"));

            env["owin.Version"] = "1.1";
            Assert.That(request.Version, Is.EqualTo("1.1"));

            env["owin.Version"] = null;
            Assert.That(request.Version, Is.Null);

            env.Remove("owin.Version");
            Assert.That(request.Version, Is.Null);
        }

        [Test]
        public void All_environment_variables_from_spec_are_available_as_typed_properties()
        {
            //"owin.RequestMethod" 	A string containing the HTTP request method of the request (e.g., "GET", "POST").
            //"owin.RequestUri" 	A string containing the HTTP request URI of the request. The value must include the query string of the HTTP request URI (e.g., "/path/and?query=string"). The URI must be relative to the application delegate; see Paths.
            //"owin.RequestHeaders" 	An instance of IDictionary<string, string> which represents the HTTP headers present in the request (the request header dictionary); see Headers.
            //"owin.RequestBody" 	An instance of the body delegate representing the body of the request. May be null.
            //"owin.BaseUri" 	A string containing the portion of the request URI's path corresponding to the "root" of the application object. See Paths.
            //"owin.ServerName", "owin.ServerPort" 	Hosts should provide values which can be used to reconstruct the full URI of the request in absence of the HTTP Host header of the request.
            //"owin.UriScheme" 	A string representing the URI scheme (e.g. "http", "https")
            //"owin.RemoteEndPoint" 	A System.Net.IPEndPoint representing the connected client.
            //"owin.Version" 	The string "1.0" indicating OWIN version 1.0. 

            var headers = new Dictionary<string, string>();
            BodyDelegate body = (next, error, complete) => () => { };
            var env = new Dictionary<string, object>
            {
                {"owin.RequestMethod", "GET"},
                {"owin.RequestUri", "/foo"},
                {"owin.RequestHeaders", headers},
                {"owin.RequestBody", body},
                {"owin.BaseUri", "/my-app"},
                {"owin.ServerName", "localhost"},
                {"owin.ServerPort", "8080"},
                {"owin.UriScheme", "https"},
                {"owin.RemoteEndPoint", new IPEndPoint(IPAddress.Parse("127.0.0.1") ?? IPAddress.None, 80)},
                {"owin.Version", "1.0"},
            };

            var request = new Request(env);
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.RequestUri, Is.EqualTo("/foo"));
            Assert.That(request.Headers, Is.SameAs(headers));
            Assert.That(request.Body, Is.SameAs(body));
            Assert.That(request.BaseUri, Is.EqualTo("/my-app"));
            Assert.That(request.ServerName, Is.EqualTo("localhost"));
            Assert.That(request.ServerPort, Is.EqualTo("8080"));
            Assert.That(request.UriScheme, Is.EqualTo("https"));
            Assert.That(request.RemoteEndPoint.ToString(), Is.EqualTo("127.0.0.1:80"));
            Assert.That(request.Version, Is.EqualTo("1.0"));
        }
    }
}