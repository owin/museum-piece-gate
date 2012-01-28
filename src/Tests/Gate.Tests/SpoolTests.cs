using System;
using System.Linq;
using System.Text;
using Gate.Utils;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Gate.Tests
{
    [TestFixture]
    public class SpoolTests
    {
        static ArraySegment<byte> Data(int count)
        {
            return new ArraySegment<byte>(new byte[count], 0, count);
        }


        static ArraySegment<byte> Data(int count, string fill)
        {
            var data = Data(count);
            var fillBytes = Encoding.UTF8.GetBytes(fill);
            var offset = data.Offset;
            while (offset < data.Count)
            {
                var copy = Math.Min(data.Count - offset, fillBytes.Length);
                Array.Copy(fillBytes, 0, data.Array, offset, copy);
                offset += copy;
            }
            return data;
        }

        void AssertDataEqual(ArraySegment<byte> data, ArraySegment<byte> equals)
        {
            Assert.That(data.Offset, Is.EqualTo(equals.Offset));
            Assert.That(data.Count, Is.EqualTo(equals.Count));

            var d1 = data.Array.Skip(data.Offset).Take(data.Count);
            var d2 = equals.Array.Skip(equals.Offset).Take(equals.Count);
            var diffs = d1.Zip(d2, (b1, b2) => new {b1, b2})
                .Select((bb, i) => new {bb.b1, bb.b2, i})
                .Where(bb => bb.b1 != bb.b2);
            Assert.That(!diffs.Any(), diffs.Count() + " differences " + diffs.Take(10).Aggregate("", (str, bb) => str + "\r\n[" + bb.i + "] actual " + bb.b1 + " != expected " + bb.b2));
        }

        [Test]
        public void Pushing_data_with_no_continuation_is_synchronous()
        {
            var spool = new Spool();
            var delayed = spool.Push(Data(100), null);
            Assert.That(delayed, Is.False);
        }

        [Test]
        public void Pushing_data_with_continuation_is_asynchronous()
        {
            var spool = new Spool();
            var delayed = spool.Push(Data(100), () => { });
            Assert.That(delayed, Is.True);
        }

        [Test]
        public void Pulling_data_when_spooled_is_synchronous()
        {
            var spool = new Spool();
            var asyncPush = spool.Push(Data(100, "hello"), null);
            Assert.That(asyncPush, Is.False);

            var data = Data(100);
            var asyncPull = spool.Pull(data, new int[1], () => { });
            Assert.That(asyncPull, Is.False);

            AssertDataEqual(data, Data(100, "hello"));
        }

        [Test]
        public void Pulling_before_pushing_is_asynchronous()
        {
            var spool = new Spool();
            var data = Data(100);
            var callbackPull = false;
            var asyncPull = spool.Pull(data, new int[1], () => callbackPull = true);
            Assert.That(asyncPull, Is.True);
            Assert.That(callbackPull, Is.False);

            var asyncPush = spool.Push(Data(100, "hello"), null);
            Assert.That(asyncPush, Is.False);
            Assert.That(callbackPull, Is.True);

            AssertDataEqual(data, Data(100, "hello"));
        }

        [Test]
        public void Pushing_async_before_pulling_async()
        {
            var spool = new Spool();
            var data = Data(100);

            var callbackPush = false;
            var asyncPush = spool.Push(Data(100, "hello"), () => callbackPush = true);
            Assert.That(asyncPush, Is.True);
            Assert.That(callbackPush, Is.False);

            var callbackPull = false;
            var asyncPull = spool.Pull(data, new int[1], () => callbackPull = true);
            Assert.That(asyncPull, Is.False);
            Assert.That(callbackPull, Is.False);
            Assert.That(callbackPush, Is.True);

            AssertDataEqual(data, Data(100, "hello"));
        }

        [Test]
        public void Pushing_odd_sizes_completes_partially()
        {
            var spool = new Spool();

            // push 100 (delayed)
            var callbackPushOne = false;
            var asyncPushOne = spool.Push(Data(100, "hello"), () => callbackPushOne = true);
            Assert.That(asyncPushOne, Is.True);
            Assert.That(callbackPushOne, Is.False);

            // pull 50 (immediate)
            var dataOne = Data(50);
            var callbackPullOne = false;
            var asyncPullOne = spool.Pull(dataOne, new int[1], () => callbackPullOne = true);
            Assert.That(asyncPullOne, Is.False);
            Assert.That(callbackPullOne, Is.False);
            Assert.That(callbackPushOne, Is.False);

            AssertDataEqual(dataOne, Data(50, "hello"));

            // pull 100 (delayed, and release first push)
            var dataTwo = Data(100);
            var callbackPullTwo = false;
            var asyncPullTwo = spool.Pull(dataTwo, new int[1], () => callbackPullTwo = true);
            Assert.That(asyncPullTwo, Is.True);
            Assert.That(callbackPullTwo, Is.False);
            Assert.That(callbackPushOne, Is.True);

            // push 50 (immediate, and releases second pull)
            var callbackPushTwo = false;
            var asyncPushTwo = spool.Push(Data(50, "hello"), () => callbackPushTwo = true);
            Assert.That(asyncPushTwo, Is.False);
            Assert.That(callbackPushTwo, Is.False);
            Assert.That(callbackPullTwo, Is.True);

            AssertDataEqual(dataTwo, Data(100, "hello"));

            //final state
            Assert.That(asyncPushOne, Is.True);
            Assert.That(callbackPushOne, Is.True);
            Assert.That(asyncPullOne, Is.False);
            Assert.That(callbackPullOne, Is.False);
            Assert.That(asyncPullTwo, Is.True);
            Assert.That(callbackPullTwo, Is.True);
            Assert.That(asyncPushTwo, Is.False);
            Assert.That(callbackPushTwo, Is.False);
        }

        [Test]
        public void Completing_ends_partial_pull()
        {
            var spool = new Spool();
            spool.Push(Data(50, "hello"), null);

            var data = Data(200);
            var retval = new int[1];
            var callbackPull = false;
            var asyncPull = spool.Pull(data, retval, () => callbackPull = true);
            Assert.That(asyncPull, Is.True);
            Assert.That(callbackPull, Is.False);

            spool.Push(Data(50, "hello"), null);
            Assert.That(callbackPull, Is.False);

            spool.PushComplete();
            Assert.That(callbackPull, Is.True);
            Assert.That(retval[0], Is.EqualTo(100));
        }

        [Test]
        public void Completing_makes_further_pulls_return_with_nothing()
        {
            var spool = new Spool();
            spool.PushComplete();

            var data = Data(200);
            var retval = new int[1];
            var callbackPull = false;
            var asyncPull = spool.Pull(data, retval, () => callbackPull = true);
            Assert.That(asyncPull, Is.False);
            Assert.That(callbackPull, Is.False);
            Assert.That(retval[0], Is.EqualTo(0));
        }

        [Test]
        public void Eager_spool_returns_partial_pull_on_push()
        {
            var spool = new Spool(true);

            var data = Data(200);
            var retval = new int[1];
            var callbackPull = false;
            var asyncPull = spool.Pull(data, retval, () => callbackPull = true);
            Assert.That(asyncPull, Is.True);
            Assert.That(callbackPull, Is.False);

            spool.Push(Data(50, "hello"), null);
            Assert.That(callbackPull, Is.True);
            Assert.That(retval[0], Is.EqualTo(50));
        }

        [Test]
        public void Eager_spool_returns_partial_pull_immediately_if_push_spooled()
        {
            var spool = new Spool(true);

            spool.Push(Data(50, "hello"), null);

            var data = Data(200);
            var retval = new int[1];
            var callbackPull = false;
            var asyncPull = spool.Pull(data, retval, () => callbackPull = true);
            Assert.That(asyncPull, Is.False);
            Assert.That(callbackPull, Is.False);
            Assert.That(retval[0], Is.EqualTo(50));
        }
        
        [Test]
        public void Eager_spool_returns_partial_pull_immediately_if_push_pending()
        {
            var spool = new Spool(true);

            var callbackPush = false;
            var asyncPush = spool.Push(Data(50, "hello"), () => callbackPush = true);
            Assert.That(asyncPush, Is.True);
            Assert.That(callbackPush, Is.False);

            var data = Data(200);
            var retval = new int[1];
            var callbackPull = false;
            var asyncPull = spool.Pull(data, retval, () => callbackPull = true);
            Assert.That(asyncPull, Is.False);
            Assert.That(callbackPull, Is.False);
            Assert.That(retval[0], Is.EqualTo(50));

            Assert.That(callbackPush, Is.True);
        }
    }
}