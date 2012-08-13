using System;
using System.IO;
using NUnit.Framework;
using Owin;
using Owin.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class StaticTests
    {
        private ResultParameters Call(Action<IAppBuilder> pipe, string path)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = builder.Build<AppDelegate>();
            return app(new Request() { Path = path }.Call).Result;
        }

        [Test]
        public void Static_serves_files_from_default_location()
        {
            var result = Call(b => b.UseStatic(), "/kayak.png");

            Assert.That(result.Status, Is.EqualTo(200));
        }

        [Test]
        public void Static_serves_files_from_provided_location()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = Call(b => b.UseStatic(root), "/kayak.png");

            Assert.That(result.Status, Is.EqualTo(200));
        }

        [Test]
        public void Static_serves_files_from_provided_whitelist()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = Call(b => b.UseStatic(root, new[] { "/scripts/lib.js" }), "/scripts/lib.js");

            Assert.That(result.Status, Is.EqualTo(200));
        }

        [Test]
        public void Static_returns_404_for_request_to_file_not_in_provided_whitelist()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = Call(b => b.UseStatic(root, new[] { "/scripts/lib.js" }), "/kayak.png");

            Assert.That(result.Status, Is.EqualTo(404));
        }

        [Test]
        public void Static_calls_down_the_chain_if_URL_root_is_unknown()
        {
            var result = Call(b => b.UseStatic().UseFunc<AppDelegate>(_=>call => new Response(301).EndAsync()), "/johnson/and/johnson");

            Assert.That(result.Status, Is.EqualTo(301));
        }

        [Test]
        public void Static_returns_404_on_missing_file()
        {
            var result = Call(b => b.UseStatic(), "/scripts/penicillin.js");

            Assert.That(result.Status, Is.EqualTo(404));
        }
    }
}