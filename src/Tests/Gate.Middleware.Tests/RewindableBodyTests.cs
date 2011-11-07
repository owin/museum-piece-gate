using System;
using System.Linq;
using System.Text;
using Gate.Owin;
using NUnit.Framework;
using Gate.TestHelpers;
using Gate.Middleware;

namespace Gate.Middleware.Tests
{
// ReSharper disable InconsistentNaming
    [TestFixture]
    public class RewindableBodyTests
    {
        [Test]
        public void Calling_wrap_should_return_non_null_delegate_if_argument_not_null()
        {
            BodyDelegate body = delegate(Func<ArraySegment<byte>, Action, bool> next, Action<Exception> error, Action complete)
            {
                complete();
                return () => { };
            };
            var wrapped = RewindableBody.Wrap(body);
            Assert.That(wrapped, Is.Not.Null);
        }

        [Test]
        public void Calling_wrap_should_return_null_delegate_if_argument_null()
        {
            var wrapped = RewindableBody.Wrap((BodyDelegate) null);
            Assert.That(wrapped, Is.Null);
        }

        [Test]
        public void Call_should_pass_through_only_once_and_produce_same_results()
        {
            var bodyCallCount = 0;
            BodyDelegate body = (next, error, complete) =>
            {
                ++bodyCallCount;
                next(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello world")), null);
                complete();
                return () => { };
            };
            var wrapped = RewindableBody.Wrap(body);

            var consumer1 = new FakeConsumer(false);
            consumer1.InvokeBodyDelegate(wrapped, true);

            var consumer2 = new FakeConsumer(false);
            consumer2.InvokeBodyDelegate(wrapped, true);

            Assert.That(bodyCallCount, Is.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(consumer1.ConsumedData), Is.EqualTo("Hello world"));
            Assert.That(Encoding.UTF8.GetString(consumer2.ConsumedData), Is.EqualTo("Hello world"));
        }


        [Test]
        public void Huge_call_should_also_pass_through_once()
        {
            var bodyCallCount = 0;
            var totalBytes = 0;
            BodyDelegate body = (next, error, complete) =>
            {
                ++bodyCallCount;
                foreach (var line in Enumerable.Range(0, 4000))
                {
                    var bytes = Encoding.UTF8.GetBytes("Hello line " + line);
                    totalBytes += bytes.Length;
                    next(new ArraySegment<byte>(bytes), null);
                }
                complete();
                return () => { };
            };
            var wrapped = RewindableBody.Wrap(body);

            var consumer1 = new FakeConsumer(false);
            consumer1.InvokeBodyDelegate(wrapped, true);

            var consumer2 = new FakeConsumer(false);
            consumer2.InvokeBodyDelegate(wrapped, true);

            Assert.That(bodyCallCount, Is.EqualTo(1));
            Assert.That(consumer1.ConsumedData.Length, Is.EqualTo(totalBytes));
            Assert.That(consumer2.ConsumedData.Length, Is.EqualTo(totalBytes));
        }
    }
}