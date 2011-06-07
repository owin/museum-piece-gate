using System;
using System.Collections.Generic;
using System.Linq;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Sample.App.Tests
{
    [TestFixture]
    public class DefaultPageTests
    {
        FakeHost _host;

        [SetUp]
        public void Init()
        {
            _host = new FakeHost("Sample.App.Startup");
        }

        [Test]
        public void Default_page_appears_at_root()
        {
            var response = _host.GET("/");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("<h1>Sample.App</h1>"));
            Assert.That(response.BodyText, Is.StringContaining("Wilson"));
            Assert.That(response.BodyText, Is.StringContaining("Nancy"));
        }
    }
}
