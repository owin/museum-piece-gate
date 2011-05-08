using Gate.TestHelpers;
using NUnit.Framework;

namespace Sample.App.Tests
{
    [TestFixture]
    public class WilsonMapTests
    {
        FakeHost _host;

        [SetUp]
        public void Init()
        {
            _host = new FakeHost("Sample.App.Startup");
        }

        [Test]
        public void Wilson_is_running_at_wilson_path()
        {
            var response = _host.GET("/wilson");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("left - right"));
        }

        [Test]
        public void Flip_left_will_reverse_text()
        {
            var response = _host.GET("/wilson?flip=left");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("thgir - tfel"));
        }

        [Test]
        public void Crash_should_produce_ShowExceptions_page()
        {
            var response = _host.GET("/wilson?flip=crash");

            Assert.That(response.Status, Is.EqualTo("500 Internal Server Error"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("<title>ApplicationException at /wilson</title>"));
        }


        [Test]
        public void Async_Wilson_is_running_at_wilsonasync_path()
        {
            var response = _host.GET("/wilsonasync");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("left - right"));
        }

        [Test]
        public void Async_Flip_left_will_reverse_text()
        {
            var response = _host.GET("/wilsonasync?flip=left");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("thgir - tfel"));
        }

        [Test, Ignore("Exception in body isn't resuming execution for some reason")]
        public void Async_Crash_should_produce_ShowExceptions_page()
        {
            var response = _host.GET("/wilsonasync?flip=crash");

            Assert.That(response.Status, Is.EqualTo("500 Internal Server Error"));
            Assert.That(response.Headers["Content-Type"], Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("<title>ApplicationException at /wilson</title>"));
        }
    }
}