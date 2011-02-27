using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gato;

namespace Gato.Tests
{
    [TestFixture]
    public class OutputStreamTests
    {
        OutputStream stream;
        bool completed;
        StringBuilder sb;
        Func<ArraySegment<byte>, Action, bool> next;
        Action complete;

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
                return true;
            };
        }

        void WriteString(string str)
        {
            var b = Encoding.ASCII.GetBytes(str);
            stream.Write(b, 0, b.Length);
        }

        bool BeginWriteString(string str, Action<IAsyncResult> callback)
        {
            var b = Encoding.ASCII.GetBytes(str);
            return stream.BeginWrite(b, 0, b.Length, iasr => callback(iasr), null).CompletedSynchronously;
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


            Assert.IsTrue(BeginWriteString("asdf", iasr => { stream.EndWrite(iasr); }));
            Assert.IsTrue(BeginWriteString("jkl;", iasr => { stream.EndWrite(iasr); }));
            Assert.IsTrue(BeginWriteString("lol", iasr => { stream.EndWrite(iasr); }));
            stream.Dispose();

            Assert.IsTrue(completed);
            Assert.AreEqual("asdfjkl;lol", sb.ToString());
        }
    }
}
