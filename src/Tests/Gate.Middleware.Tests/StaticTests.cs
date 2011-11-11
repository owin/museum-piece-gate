using System;
using System.IO;
using Gate.Builder;
using Gate.Owin;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class StaticTests
    {
        [Test]
        public void Static_serves_files_from_default_location()
        {
            var result = AppUtils.CallPipe(b =>
                b.Static(), FakeHostRequest.GetRequest("/kayak.png"));

            Assert.That(result.Status, Is.EqualTo("200 OK"));
        }

        [Test]
        public void Static_serves_files_from_provided_location()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = AppUtils.CallPipe(b =>
                b.Static(root), FakeHostRequest.GetRequest("/kayak.png"));

            Assert.That(result.Status, Is.EqualTo("200 OK"));
        }

        [Test]
        public void Static_serves_files_from_provided_whitelist()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = AppUtils.CallPipe(b =>
                b.Static(root, new[] {"/scripts/lib.js"}), FakeHostRequest.GetRequest("/scripts/lib.js"));

            Assert.That(result.Status, Is.EqualTo("200 OK"));
        }

        [Test]
        public void Static_returns_404_for_request_to_file_not_in_provided_whitelist()
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            var result = AppUtils.CallPipe(b =>
                b.Static(root, new[] { "/scripts/lib.js" }), FakeHostRequest.GetRequest("/kayak.png"));

            Assert.That(result.Status, Is.EqualTo("404 Not Found"));
        }

        [Test]
        public void Static_calls_down_the_chain_if_URL_root_is_unknown()
        {
            var app = new FakeApp("200 OK", "Hello World");
            app.Headers["Content-Type"] = "text/plain";
            var config = AppBuilder.BuildConfiguration(b => b.Static().Run(app.AppDelegate));
            var host = new FakeHost(config);
            var response = host.GET("/johnson/and/johnson");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello World"));
            Assert.That(app.AppDelegateInvoked, Is.True);
        }

        [Test]
        public void Static_returns_404_on_missing_file()
        {
            var result = AppUtils.CallPipe(b =>
                b.Static(), FakeHostRequest.GetRequest("/scripts/penicillin.js"));

            Assert.That(result.Status, Is.EqualTo("404 Not Found"));
        }
    }
}