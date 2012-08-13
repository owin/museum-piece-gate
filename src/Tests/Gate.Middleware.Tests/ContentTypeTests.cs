using System;
using NUnit.Framework;
using Owin;
using Owin.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ContentTypeTests
    {
        private ResultParameters Call(Action<IAppBuilder> pipe)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppDelegate)builder.Build(typeof(AppDelegate));
            return app(new Request().Call).Result;
        }

        [Test]
        public void Should_set_Content_Type_to_default_text_html_if_none_is_set()
        {
            var callResult = Call(b => b
                .UseContentType()
                .UseDirect((request, response) => response.EndAsync()));
            Assert.That(callResult.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
        }

        [Test]
        public void Should_set_Content_Type_to_chosen_default_if_none_is_set()
        {
            var callResult = Call(b => b
                .UseContentType("application/octet-stream")
                .UseDirect((request, response) => response.EndAsync()));

            Assert.That(callResult.Headers.GetHeader("Content-Type"), Is.EqualTo("application/octet-stream"));
        }

        [Test]
        public void Should_not_change_Content_Type_if_it_is_already_set()
        {
            var callResult = Call(b => b
                .UseContentType()
                .UseDirect((request, response) =>
                {
                    response.SetHeader("CONTENT-Type", "foo/bar");
                    return response.EndAsync();
                }));

            Assert.That(callResult.Headers.GetHeader("Content-Type"), Is.EqualTo("foo/bar"));
        }

        [Test]
        public void Should_detect_Content_Type_case_insensitive()
        {
            var callResult = Call(b => b
                .UseContentType()
                .UseDirect((request, response) =>
                {
                    response.SetHeader("CONTENT-Type", "foo/bar");
                    return response.EndAsync();
                }));

            Assert.That(callResult.Headers.GetHeader("CONTENT-Type"), Is.EqualTo("foo/bar"));
        }
    }
}