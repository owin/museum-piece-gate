using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Startup;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Sample.App.Tests
{
    [TestFixture]
    public class DefaultPageTests
    {
        [Test]
        public void Default_page_appears_at_root()
        {
            var app = new AppBuilder()
                .Configure(new Startup().Configuration)
                .Build();

            var callResult = AppUtils.Call(app);

            Assert.That(callResult.Status, Is.EqualTo("200 OK"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("<h1>Sample.App</h1>"));
            Assert.That(callResult.BodyText, Is.StringContaining("Wilson"));
            Assert.That(callResult.BodyText, Is.StringContaining("Nancy"));
        }
    }
}
