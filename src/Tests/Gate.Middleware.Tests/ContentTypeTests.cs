using System;
using NUnit.Framework;
using Owin;
using Owin.Builder;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class ContentTypeTests
    {

        private IDictionary<string, string[]> Call(Action<IAppBuilder> pipe)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppFunc)builder.Build(typeof(AppFunc));
            var env = new Request().Environment;
            var resp = new Response(env);
            resp.OutputStream = new MemoryStream();
            app(env).Wait();
            return resp.Headers;
        }

        [Test]
        public void Should_set_Content_Type_to_default_text_html_if_none_is_set()
        {
            var responseHeaders = Call(b => b
                .UseContentType()
                .UseGate((request, response) => { }));
            Assert.That(responseHeaders.GetHeader("Content-Type"), Is.EqualTo("text/html"));
        }

        [Test]
        public void Should_set_Content_Type_to_chosen_default_if_none_is_set()
        {
            var responseHeaders = Call(b => b
                .UseContentType("application/octet-stream")
                .UseGate((request, response) => { }));

            Assert.That(responseHeaders.GetHeader("Content-Type"), Is.EqualTo("application/octet-stream"));
        }

        [Test]
        public void Should_not_change_Content_Type_if_it_is_already_set()
        {
            var responseHeaders = Call(b => b
                .UseContentType()
                .UseGate((request, response) =>
                {
                    response.SetHeader("CONTENT-Type", "foo/bar");
                }));

            Assert.That(responseHeaders.GetHeader("Content-Type"), Is.EqualTo("foo/bar"));
        }

        [Test]
        public void Should_detect_Content_Type_case_insensitive()
        {
            var responseHeaders = Call(b => b
                .UseContentType()
                .UseGate((request, response) =>
                {
                    response.SetHeader("CONTENT-Type", "foo/bar");
                }));

            Assert.That(responseHeaders.GetHeader("CONTENT-Type"), Is.EqualTo("foo/bar"));
        }
    }
}