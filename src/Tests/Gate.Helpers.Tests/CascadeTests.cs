using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Owin;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Helpers.Tests
{
    [TestFixture]
    public class CascadeTests
    {
        [Test]
        public void Cascade_with_no_apps_should_return_404()
        {
            var cascade = Cascade.Middleware(Enumerable.Empty<AppDelegate>());
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("404 Not Found"));
        }

        [Test]
        public void Cascade_with_app_calls_through()
        {
            var app = new FakeApp("200 OK", "Hello world");
            app.Headers["Content-Type"] = "text/plain";
            var cascade = Cascade.Middleware(app.AppDelegate);
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
            app2.Headers["Content-Type"] = "text/plain";
            var app3 = new FakeApp("404 Not Found", "");
            var cascade = Cascade.Middleware(app1.AppDelegate, app2.AppDelegate, app3.AppDelegate);
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
            app2.Headers["Content-Type"] = "text/plain";
            var app3 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var cascade = Cascade.Middleware(app1.AppDelegate, app2.AppDelegate, app3.AppDelegate);
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