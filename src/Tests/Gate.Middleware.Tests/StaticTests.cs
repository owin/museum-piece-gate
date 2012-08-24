using System;
using System.IO;
using NUnit.Framework;
using Owin;
using Owin.Builder;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class StaticTests
    {
        private int Call(Action<IAppBuilder> pipe, string path)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = builder.Build<AppFunc>();

            Request request = new Request() { Path = path };
            Response response = new Response(request.Environment);
            response.OutputStream = new MemoryStream();
            app(request.Environment).Wait();
            return response.StatusCode;
        }

        [Test]
        public void Static_serves_files_from_default_location()
        {
            var result = Call(b => b.UseStatic(), "/kayak.png");

            Assert.That(result, Is.EqualTo(200));
        }

        [Test]
        public void Static_serves_files_from_provided_location()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = Call(b => b.UseStatic(root), "/kayak.png");

            Assert.That(result, Is.EqualTo(200));
        }

        [Test]
        public void Static_serves_files_from_provided_whitelist()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = Call(b => b.UseStatic(root, new[] { "/scripts/lib.js" }), "/scripts/lib.js");

            Assert.That(result, Is.EqualTo(200));
        }

        [Test]
        public void Static_returns_404_for_request_to_file_not_in_provided_whitelist()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = Call(b => b.UseStatic(root, new[] { "/scripts/lib.js" }), "/kayak.png");

            Assert.That(result, Is.EqualTo(404));
        }

        [Test]
        public void Static_calls_down_the_chain_if_URL_root_is_unknown()
        {
            var result = Call(b => b.UseStatic().UseFunc<AppFunc>(
                _=> env => new Response(env) { StatusCode = 301 }.EndAsync()), "/johnson/and/johnson");

            Assert.That(result, Is.EqualTo(301));
        }

        [Test]
        public void Static_returns_404_on_missing_file()
        {
            var result = Call(b => b.UseStatic(), "/scripts/penicillin.js");

            Assert.That(result, Is.EqualTo(404));
        }
    }
}