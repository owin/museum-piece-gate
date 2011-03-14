using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;

namespace Gate.Tests
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

    [TestFixture]
    public class EnvironmentTests
    {
        [Test]
        public void Version_property_provide_access_to_environment()
        {
            var env = new Dictionary<string, object> {{"owin.Version", "1.0"}};
            var environment = new Environment(env);
            Assert.That(environment.Version, Is.EqualTo("1.0"));
        }

        [Test]
        public void Envoronment_access_is_not_buffered_or_cached()
        {
            var env = new Dictionary<string, object> {{"owin.Version", "1.0"}};
            var environment = new Environment(env);
            Assert.That(environment.Version, Is.EqualTo("1.0"));

            env["owin.Version"] = "1.1";
            Assert.That(environment.Version, Is.EqualTo("1.1"));

            env["owin.Version"] = null;
            Assert.That(environment.Version, Is.Null);

            env.Remove("owin.Version");
            Assert.That(environment.Version, Is.Null);
        }

        [Test]
        public void All_environment_variables_from_spec_are_available_as_typed_properties()
        {
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

            var environment = new Environment(env);
            Assert.That(environment.Method, Is.EqualTo("GET"));
            Assert.That(environment.RequestUri, Is.EqualTo("/foo"));
            Assert.That(environment.Headers, Is.SameAs(headers));
            Assert.That(environment.Body, Is.SameAs(body));
            Assert.That(environment.BaseUri, Is.EqualTo("/my-app"));
            Assert.That(environment.ServerName, Is.EqualTo("localhost"));
            Assert.That(environment.ServerPort, Is.EqualTo("8080"));
            Assert.That(environment.UriScheme, Is.EqualTo("https"));
            Assert.That(environment.RemoteEndPoint.ToString(), Is.EqualTo("127.0.0.1:80"));
            Assert.That(environment.Version, Is.EqualTo("1.0"));
        }

        [Test]
        public void Environment_properties_may_be_used_to_initialize_env_dictionary()
        {
            var headers = new Dictionary<string, string>();
            BodyDelegate body = (next, error, complete) => () => { };

            var env = new Dictionary<string, object>();
            var environment = new Environment(env)
            {
                Method="GET",
                RequestUri="/foo",
                Headers=headers,
                Body=body,
                BaseUri="/my-app",
                ServerName="localhost",
                ServerPort="8080",
                UriScheme="https",
                RemoteEndPoint=new IPEndPoint(IPAddress.Parse("127.0.0.1"), 80),
                Version = "1.0"
            };
            
            Assert.That(environment.Method, Is.EqualTo("GET"));
            Assert.That(environment.RequestUri, Is.EqualTo("/foo"));
            Assert.That(environment.Headers, Is.SameAs(headers));
            Assert.That(environment.Body, Is.SameAs(body));
            Assert.That(environment.BaseUri, Is.EqualTo("/my-app"));
            Assert.That(environment.ServerName, Is.EqualTo("localhost"));
            Assert.That(environment.ServerPort, Is.EqualTo("8080"));
            Assert.That(environment.UriScheme, Is.EqualTo("https"));
            Assert.That(environment.RemoteEndPoint.ToString(), Is.EqualTo("127.0.0.1:80"));
            Assert.That(environment.Version, Is.EqualTo("1.0"));
            
            Assert.That(env["owin.RequestMethod"], Is.EqualTo("GET"));
            Assert.That(env["owin.RequestUri"], Is.EqualTo("/foo"));
            Assert.That(env["owin.RequestHeaders"], Is.SameAs(headers));
            Assert.That(env["owin.RequestBody"], Is.SameAs(body));
            Assert.That(env["owin.BaseUri"], Is.EqualTo("/my-app"));
            Assert.That(env["owin.ServerName"], Is.EqualTo("localhost"));
            Assert.That(env["owin.ServerPort"], Is.EqualTo("8080"));
            Assert.That(env["owin.UriScheme"], Is.EqualTo("https"));
            Assert.That(env["owin.RemoteEndPoint.ToString()"], Is.EqualTo("127.0.0.1:80"));
            Assert.That(env["owin.Version"], Is.EqualTo("1.0"));
        }
    }
}