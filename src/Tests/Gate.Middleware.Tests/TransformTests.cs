using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Owin;
using Gate.Builder;
using Gate.TestHelpers;
using NUnit.Framework;
using Gate.Middleware;

namespace Gate.Middleware.Tests
{
    using Response = Tuple<string, IDictionary<string, IEnumerable<string>>, BodyDelegate>;

    [TestFixture]
    class TransformTests
    {
        [Test]
        public void Request_is_transformed()
        {
            var result = AppUtils.CallPipe(b => b
                .Transform((e, c, ex) => { e.Method = "100 Transformed"; c(e); })
                .Run(AppUtils.ShowEnvironment));

            Assert.That(result.BodyXml.Element(Environment.RequestMethodKey).Value, Is.EqualTo("100 Transformed"));
        }

        [Test]
        public void Request_transform_exception_is_caught_and_forwarded()
        {
            var result = AppUtils.CallPipe(b => b
                .Transform((Environment e, Action<Environment> c, Action<Exception> ex) => { throw new Exception("Request_transform_exception_thrown"); })
                .Simple());

            Assert.That(result.Exception.Message, Is.EqualTo("Request_transform_exception_thrown"));
        }

        [Test]
        public void Request_transform_exception_is_forwarded()
        {
            var result = AppUtils.CallPipe(b => b
                .Transform((Environment e, Action<Environment> c, Action<Exception> ex) => ex(new Exception("Request_transform_exception_forwarded")))
                .Simple());

            Assert.That(result.Exception.Message, Is.EqualTo("Request_transform_exception_forwarded"));
        }

        [Test]
        public void Response_is_transformed()
        {
            var result = AppUtils.CallPipe(b => b
                .Transform((e, c, ex) => { c(Tuple.Create("999 Transformed", e.Item2, e.Item3)); })
                .Simple());

            Assert.That(result.Status, Is.EqualTo("999 Transformed"));
        }

        [Test]
        public void Response_transform_exception_is_caught_and_forwarded()
        {
            var result = AppUtils.CallPipe(b => b
                .Transform((Response e, Action<Response> c, Action<Exception> ex) => { throw new Exception("Request_transform_exception_thrown"); })
                .Simple());

            Assert.That(result.Exception.Message, Is.EqualTo("Request_transform_exception_thrown"));
        }

        [Test]
        public void Response_transform_exception_is_forwarded()
        {
            var result = AppUtils.CallPipe(b => b
                .Transform((Response e, Action<Response> c, Action<Exception> ex) => ex(new Exception("Request_transform_exception_forwarded")))
                .Simple());

            Assert.That(result.Exception.Message, Is.EqualTo("Request_transform_exception_forwarded"));
        }
    }
}
