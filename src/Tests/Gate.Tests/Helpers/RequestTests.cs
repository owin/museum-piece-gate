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
            new Owin(env) {QueryString = "foo=bar"};

            var request = new Request(env);
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Changing_QueryString_in_environment_reparses_Query_dictionary()
        {
            var env = new Dictionary<string, object>();
            new Owin(env) {QueryString = "foo=bar"};

            var request = new Request(env);
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));

            new Owin(env) {QueryString = "foo=quux"};
            Assert.That(request.Query["foo"], Is.EqualTo("quux"));
        }
    }
}