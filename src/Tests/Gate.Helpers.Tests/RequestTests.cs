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
    }
}