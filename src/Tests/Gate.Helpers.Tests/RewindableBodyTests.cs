using System;
using System.Text;
using Nancy.Hosting.Owin.Tests.Fakes;
using NUnit.Framework;

namespace Gate.Helpers.Tests
{
// ReSharper disable InconsistentNaming
    [TestFixture]
    public class RewindableBodyTests
    {
        [Test]
        public void Calling_wrap_should_return_non_null_delegate_if_argument_not_null()
        {
            BodyDelegate body = (next, error, complete) =>
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
    }
}