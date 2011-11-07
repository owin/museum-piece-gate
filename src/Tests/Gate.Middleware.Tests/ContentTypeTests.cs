using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.TestHelpers;
using NUnit.Framework;
using Gate.Middleware;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ContentTypeTests
    {
        [Test]
        public void Should_set_Content_Type_to_default_text_html_if_none_is_set()
        {
            var app = AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello World!");
            var callResult = AppUtils.Call(ContentType.Middleware(app));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
        }

        [Test]
        public void Should_set_Content_Type_to_chosen_default_if_none_is_set()
        {
            var app = AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello World!");
            var callResult = AppUtils.Call(ContentType.Middleware(app, "application/octet-stream"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("application/octet-stream"));
        }

        [Test]
        public void Should_not_change_Content_Type_if_it_is_already_set()
        {
            var app = AppUtils.Simple("200 OK", new Dictionary<string, string> {{"Content-Type", "foo/bar"}}, "Hello World!");
            var callResult = AppUtils.Call(ContentType.Middleware(app));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("foo/bar"));
        }

        [Test]
        public void Should_detect_Content_Type_case_insensitive()
        {
            var app = AppUtils.Simple("200 OK", new Dictionary<string, string> {{"CONTENT-Type", "foo/bar"}}, "Hello World!");
            var callResult = AppUtils.Call(ContentType.Middleware(app));
            Assert.That(callResult.Headers["CONTENT-Type"], Is.EqualTo("foo/bar"));
        }
    }
}