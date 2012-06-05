using System;
using System.Text;
using System.Threading;
using Gate.Middleware.StaticFiles;
using NUnit.Framework;

namespace Gate.Middleware.Tests.StaticFiles
{
    [TestFixture]
    public class BodyStreamTests
    {
        [Test]
        public void BodyStream_calls_on_data_delegate()
        {
            var called = false;
            Func<ArraySegment<byte>, Action, bool> next = (data, callback) =>
            {
                called = true;
                return false;
            };

            var bodyStream = new BodyStream(next, null, CancellationToken.None);
            bodyStream.Start(() => { }, null);

            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes("dada"));
            bodyStream.SendBytes(bytes, null, null);

            Assert.That(called, Is.True);
        }

        [Test]
        public void BodyStream_does_not_send_bytes_while_paused()
        {
            var called = false;

            Func<ArraySegment<byte>, Action, bool> write = (data, callback) =>
            {
                called = true;
                return false;
            };

            var bodyStream = new BodyStream(write, null, CancellationToken.None);
            bodyStream.Start(() => { }, null);

            bodyStream.Pause();

            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes("dada"));

            bodyStream.SendBytes(bytes, null, null);

            Assert.That(called, Is.False);
        }

        [Test]
        public void BodyStream_does_not_send_bytes_while_stopped()
        {
            var called = false;

            Func<ArraySegment<byte>, Action, bool> write = (data, callback) =>
            {
                called = true;
                return false;
            };

            var bodyStream = new BodyStream(write, null, CancellationToken.None);
            bodyStream.Start(() => { }, null);

            bodyStream.Stop();

            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes("dada"));

            bodyStream.SendBytes(bytes, null, null);

            Assert.That(called, Is.False);
        }

        [Test]
        public void BodyStream_calls_completion()
        {
            var called = false;

            var bodyStream = new BodyStream((data, callback) => false, null, CancellationToken.None);
            bodyStream.Start(() => { }, null);

            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes("dada"));

            bodyStream.SendBytes(bytes, null, () => { called = true; });

            Assert.That(called, Is.True);
        }

        [Test]
        public void BodyStream_calls_completion_if_unable_to_send_bytes()
        {
            var called = false;

            var bodyStream = new BodyStream((data, callback) => false, null, CancellationToken.None);
            bodyStream.Start(() => { }, null);

            bodyStream.Stop();

            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes("dada"));

            bodyStream.SendBytes(bytes, null, () => { called = true; });

            Assert.That(called, Is.True);
        }

        [Test]
        public void BodyStream_calls_dispose_action_when_it_finishes()
        {
            var called = false;
            var bodyStream = new BodyStream((data, callback) => false, _ => { }, CancellationToken.None);
            bodyStream.Start(() => { }, () => { called = true; });

            bodyStream.Finish();

            Assert.That(called, Is.True);
        }
    }
}