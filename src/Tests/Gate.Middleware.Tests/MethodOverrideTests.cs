using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gate.TestHelpers;
using Gate.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    class MethodOverrideTests
    {
        [Test]
        public void Method_is_overridden_if_override_present()
        {
            var result = new FakeHost(AppBuilder.BuildConfiguration(b => b
                .MethodOverride()
                .Run(AppUtils.ShowEnvironment))).Invoke(r => {
                    r.Method = "POST";
                    r.Headers = new Dictionary<string, string>() { { "x-http-method-override", "DELETE" } };
                });

            Assert.That(result.BodyXml.Element(Environment.RequestMethodKey).Value, Is.EqualTo("DELETE"));
        }

        [Test]
        public void Method_is_unchanged_if_override_not_present()
        {
            var result = new FakeHost(AppBuilder.BuildConfiguration(b => b
                .MethodOverride()
                .Run(AppUtils.ShowEnvironment))).Invoke(r =>
                {
                    r.Method = "POST";
                    r.Headers = new Dictionary<string, string>();
                });

            Assert.That(result.BodyXml.Element(Environment.RequestMethodKey).Value, Is.EqualTo("POST"));
        }
    }
}
