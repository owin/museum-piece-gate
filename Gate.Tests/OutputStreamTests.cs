using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gate;

namespace Gate.Tests
{
    [TestFixture]
    public class OutputStreamTests
    {
        OutputStream stream;
        bool completed;
        StringBuilder sb;
        Func<ArraySegment<byte>, Action, bool> next;
        Action complete;

        Action ct;

        [SetUp]
        public void SetUp()
        {
            sb = new StringBuilder();
            complete = () => completed = true;
            next = null;
            completed = false;
            stream = null;
        }

        void SynchronousConsumer()
        {
            next = (d, c) =>
            {
                sb.Append(Encoding.ASCII.GetString(d.Array, d.Offset, d.Count));
                return false;
            };
        }

        void WriteString(string str)
        {
            var b = Encoding.ASCII.GetBytes(str);
            stream.Write(b, 0, b.Length);
        }

        IAsyncResult BeginWriteString(string str, Action<IAsyncResult> callback)
        {
            var b = Encoding.ASCII.GetBytes(str);
            return stream.BeginWrite(b, 0, b.Length, iasr => callback(iasr), null);
        }

        [Test]
        public void PsCs()
        {
            SynchronousConsumer();
            stream = new OutputStream(next, complete);

            WriteString("asdf");
            WriteString("jkl;");
            WriteString("lol");
            stream.Dispose();

            Assert.IsTrue(completed);
            Assert.AreEqual("asdfjkl;lol", sb.ToString());
        }

        [Test]
        public void PaCs()
        {
            SynchronousConsumer();
            stream = new OutputStream(next, complete);

            Assert.IsTrue(BeginWriteString("asdf", iasr => { stream.EndWrite(iasr); }).CompletedSynchronously);
            Assert.IsTrue(BeginWriteString("jkl;", iasr => { stream.EndWrite(iasr); }).CompletedSynchronously);
            Assert.IsTrue(BeginWriteString("lol", iasr => { stream.EndWrite(iasr); }).CompletedSynchronously);
            stream.Dispose();

            Assert.IsTrue(completed);
            Assert.AreEqual("asdfjkl;lol", sb.ToString());
        }

        void AsynchronousConsumer()
        {
            next = (d, c) =>
            {
                sb.Append(Encoding.ASCII.GetString(d.Array, d.Offset, d.Count));

                if (c != null)
                {
                    ct = c;
                    return true;
                }
                else return false;
            };
        }

        [Test]
        public void PaCa()
        {
            AsynchronousConsumer();
            stream = new OutputStream(next, complete);

            IAsyncResult latest = null;
            var iasr0 = BeginWriteString("asdf", iasr => latest = iasr);

            ct();

            Assert.IsFalse(iasr0.CompletedSynchronously);
            Assert.AreSame(iasr0, latest);

            var iasr1 = BeginWriteString("jkl;", iasr => latest = iasr);

            ct();

            Assert.IsFalse(iasr1.CompletedSynchronously);
            Assert.AreSame(iasr1, latest);

            var iasr2 = BeginWriteString("lol", iasr => latest = iasr);

            ct();

            Assert.IsFalse(iasr2.CompletedSynchronously);
            Assert.AreSame(iasr2, latest);

            stream.Dispose();

            Assert.IsTrue(completed);
            Assert.AreEqual("asdfjkl;lol", sb.ToString());
        }

        // XXX does this ever make sense? it is equivalent to the PsCs test.
        [Test]
        public void PsCa()
        {
            AsynchronousConsumer();

            stream = new OutputStream(next, complete);

            WriteString("asdf");
            WriteString("jkl;");
            WriteString("lol");
            stream.Dispose();

            Assert.IsTrue(completed);
            Assert.AreEqual("asdfjkl;lol", sb.ToString());
        }
    }
}
