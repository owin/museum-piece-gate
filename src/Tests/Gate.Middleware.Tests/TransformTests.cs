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
    [TestFixture]
    class TransformTests
    {
        [Test]
        public void Request_is_transformed()
        {
            var result = AppUtils.Call(AppBuilder.BuildConfiguration(b => b
                .Transform((e, c) => { e.Method = "Transformed"; c(e); })
                .Run(AppUtils.ShowEnvironment)));

            Assert.That(result.BodyXml.Element(Environment.RequestMethodKey).Value, Is.EqualTo("Transformed"));
        }

        [Test]
        public void Response_is_transformed()
        {
            var result = AppUtils.Call(AppBuilder.BuildConfiguration(b => b
                .Transform((e, c) => { c(Tuple.Create("999 Transformed", e.Item2, e.Item3)); })
                .Run(AppUtils.ShowEnvironment)));

            Assert.That(result.Status, Is.EqualTo("999 Transformed"));
        }
    }
}
