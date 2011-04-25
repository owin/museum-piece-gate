using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate;
using Gate.Startup;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Sample.App.Tests
{
    [TestFixture]
    public class WilsonMapTests
    {
        static AppDelegate GetApp()
        {
            return new AppBuilder()
                .Configure(new Startup().Configuration)
                .Build();
        }

        [Test]
        public void Wilson_is_running_at_wilson_path()
        {
            var callResult = AppUtils.Call(GetApp(), "/wilson");

            Assert.That(callResult.Status, Is.EqualTo("200 OK"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("left - right"));
        }

        [Test]
        public void Flip_left_will_reverse_text()
        {
            var callResult = AppUtils.Call(GetApp(), "/wilson?flip=left");

            Assert.That(callResult.Status, Is.EqualTo("200 OK"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("thgir - tfel"));
        }

        [Test]
        public void Crash_should_produce_ShowExceptions_page()
        {
            var callResult = AppUtils.Call(GetApp(), "/wilson?flip=crash");

            Assert.That(callResult.Status, Is.EqualTo("500 Internal Server Error"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("<title>ApplicationException at /wilson</title>"));
        }

        
        [Test]
        public void Async_Wilson_is_running_at_wilsonasync_path()
        {
            var callResult = AppUtils.Call(GetApp(), "/wilsonasync");

            Assert.That(callResult.Status, Is.EqualTo("200 OK"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("left - right"));
        }

        [Test]
        public void Async_Flip_left_will_reverse_text()
        {
            var callResult = AppUtils.Call(GetApp(), "/wilsonasync?flip=left");

            Assert.That(callResult.Status, Is.EqualTo("200 OK"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("thgir - tfel"));
        }

        [Test, Ignore("Exception in body isn't resuming execution for some reason")]
        public void Async_Crash_should_produce_ShowExceptions_page()
        {
            var callResult = AppUtils.Call(GetApp(), "/wilsonasync?flip=crash");

            Assert.That(callResult.Status, Is.EqualTo("500 Internal Server Error"));
            Assert.That(callResult.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(callResult.BodyText, Is.StringContaining("<title>ApplicationException at /wilson</title>"));
        }
    }
}