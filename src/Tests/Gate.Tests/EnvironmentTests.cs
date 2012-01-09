using System;
using System.Collections.Generic;
using System.Net;
using Gate.Owin;
using NUnit.Framework;

namespace Gate.Tests
{
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
        public void Environment_access_is_not_buffered_or_cached()
        {
            var environment = new Environment() {{"owin.Version", "1.0"}};
            Assert.That(environment.Version, Is.EqualTo("1.0"));

            environment["owin.Version"] = "1.1";
            Assert.That(environment.Version, Is.EqualTo("1.1"));

            environment["owin.Version"] = null;
            Assert.That(environment.Version, Is.Null);

            environment.Remove("owin.Version");
            Assert.That(environment.Version, Is.Null);
        }

        [Test]
        public void All_environment_variables_from_spec_are_available_as_typed_properties()
        {
            var headers = new Dictionary<string, IEnumerable<string>>();
            var body = (Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>) ((next1, error1, complete1) => new BodyDelegate((next, error, complete) => () => { })(next1, error1, complete1));

            var env = new Dictionary<string, object>
            {
                {OwinConstants.RequestMethod, "GET"},
                {OwinConstants.RequestPath, "/foo"},
                {OwinConstants.RequestHeaders, headers},
                {OwinConstants.RequestBody, body},
                {OwinConstants.RequestPathBase, "/my-app"},
                {OwinConstants.RequestQueryString, "hello=world"},
                {OwinConstants.RequestScheme, "https"},
                {OwinConstants.Version, "1.0"},
            };

            var environment = new Environment(env);
            Assert.That(environment.Method, Is.EqualTo("GET"));
            Assert.That(environment.Path, Is.EqualTo("/foo"));
            Assert.That(environment.Headers, Is.SameAs(headers));
            Assert.That(environment.BodyAction, Is.SameAs(body));
            Assert.That(environment.PathBase, Is.EqualTo("/my-app"));
            Assert.That(environment.QueryString, Is.EqualTo("hello=world"));
            Assert.That(environment.Scheme, Is.EqualTo("https"));
            Assert.That(environment.Version, Is.EqualTo("1.0"));
        }

        [Test]
        public void Environment_properties_may_be_used_to_initialize_env_dictionary()
        {
            var headers = Headers.New();
            BodyDelegate body = (next, error, complete) => () => { };

            var environment = new Environment()
            {
                Method = "GET",
                Path = "/foo",
                Headers = headers,
                BodyDelegate = body,
                PathBase = "/my-app",
                QueryString = "hello=world",
                Scheme = "https",
                Version = "1.0"
            };
            IDictionary<string, object> env = environment;

            Assert.That(environment.Method, Is.EqualTo("GET"));
            Assert.That(environment.Path, Is.EqualTo("/foo"));
            Assert.That(environment.Headers, Is.SameAs(headers));
            Assert.That(environment.BodyDelegate, Is.SameAs(body));
            Assert.That(environment.PathBase, Is.EqualTo("/my-app"));
            Assert.That(environment.QueryString, Is.EqualTo("hello=world"));
            Assert.That(environment.Scheme, Is.EqualTo("https"));
            Assert.That(environment.Version, Is.EqualTo("1.0"));

            Assert.That(env["owin.RequestMethod"], Is.EqualTo("GET"));
            Assert.That(env["owin.RequestPath"], Is.EqualTo("/foo"));
            Assert.That(env["owin.RequestHeaders"], Is.SameAs(headers));
            Assert.That(env["owin.RequestBody"], Is.SameAs(body));
            Assert.That(env["owin.RequestPathBase"], Is.EqualTo("/my-app"));
            Assert.That(env["owin.RequestQueryString"], Is.EqualTo("hello=world"));
            Assert.That(env["owin.RequestScheme"], Is.EqualTo("https"));
            Assert.That(env["owin.Version"], Is.EqualTo("1.0"));
        }
    }
}