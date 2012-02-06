using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
using Gate.TestHelpers;
using NUnit.Framework;
using Gate.Middleware;
using Gate.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class CascadeTests
    {
        [Test]
        public void Cascade_with_no_apps_should_return_404()
        {
            var cascade = AppBuilder.BuildConfiguration(b => b.RunCascade(Enumerable.Empty<AppDelegate>().ToArray()));
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("404 Not Found"));
        }

        [Test]
        public void Cascade_with_app_calls_through()
        {
            var app = new FakeApp("200 OK", "Hello world");
            app.Headers.SetHeader("Content-Type", "text/plain");
            var cascade = AppBuilder.BuildConfiguration(b => b.RunCascade(app.AppDelegate));
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello world"));
            Assert.That(app.AppDelegateInvoked, Is.True);
        }

        [Test]
        public void Cascade_will_pass_along_to_first_non_404_app()
        {
            var app1 = new FakeApp("404 Not Found", "");
            var app2 = new FakeApp("200 OK", "Hello world");
            app2.Headers.SetHeader("Content-Type", "text/plain");
            var app3 = new FakeApp("404 Not Found", "");
            var cascade = AppBuilder.BuildConfiguration(b => b.RunCascade(app1.AppDelegate, app2.AppDelegate, app3.AppDelegate));
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello world"));
            Assert.That(app1.AppDelegateInvoked, Is.True);
            Assert.That(app2.AppDelegateInvoked, Is.True);
            Assert.That(app3.AppDelegateInvoked, Is.False);
        }

        [Test]
        public void Cascade_works_when_result_is_not_on_same_thread()
        {
            var app1 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var app2 = new FakeApp("200 OK", "Hello world") {SendAsync = true};
            app2.Headers.SetHeader("Content-Type", "text/plain");
            var app3 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var cascade = AppBuilder.BuildConfiguration(b => b.RunCascade(app1.AppDelegate, app2.AppDelegate, app3.AppDelegate));
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello world"));
            Assert.That(app1.AppDelegateInvoked, Is.True);
            Assert.That(app2.AppDelegateInvoked, Is.True);
            Assert.That(app3.AppDelegateInvoked, Is.False);
        }
    }
}