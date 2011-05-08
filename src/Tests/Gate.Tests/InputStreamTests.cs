using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Gate.Tests
{
    [TestFixture]
    public class InputStreamTests
    {
        ArraySegment<byte> ArrSeg(string str)
        {
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(str));
        }

        string SynchronousReadToEndOrException(ref Exception exception)
        {
            byte[] buffer = new byte[1024];
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int bytesRead = 0;

                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                }
                catch (Exception e)
                {
                    exception = e;
                    break;
                }

                if (bytesRead == 0)
                    break;

                sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }

            return sb.ToString();
        }

        Stream stream;

        void CreateStream(Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> input)
        {
            stream = new InputStream(input);
        }

        [Test]
        public void PsCs()
        {
            CreateStream((next, fault, complete) =>
            {
                next(ArrSeg("asdf"), (Action) null);
                next(ArrSeg("jkl;"), (Action) null);
                next(ArrSeg("lol"), (Action) null);
                complete();
                return () => { };
            });

            Exception e = null;
            var result = SynchronousReadToEndOrException(ref e);
            Assert.IsNull(e);
            Assert.AreEqual("asdfjkl;lol", result);
        }

        [Test, Ignore("Not yet tested")]
        public void PsCsException()
        {
            CreateStream((next, fault, complete) =>
            {
                next(ArrSeg("asdf"), (Action) null);
                next(ArrSeg("jkl;"), (Action) null);
                next(ArrSeg("lol"), (Action) null);
                fault(new Exception("ack!"));
                return () => { };
            });

            Exception e = null;
            var result = SynchronousReadToEndOrException(ref e);
            Assert.IsNotNull(e);
            Assert.AreEqual("ack!", e);
            Assert.AreEqual("asdfjkl;lol", result);
        }

        [Test]
        public void PaCs()
        {
            CreateStream((next, fault, complete) =>
            {
                next(ArrSeg("asdf"), () =>
                    next(ArrSeg("jkl;"), () =>
                        next(ArrSeg("lol"), () =>
                            complete())));
                return () => { };
            });

            Exception e = null;
            var result = SynchronousReadToEndOrException(ref e);
            Assert.IsNull(e);
            Assert.AreEqual("asdfjkl;lol", result);
        }

        [Test, Ignore("Not yet tested")]
        public void PaCsException()
        {
            CreateStream((next, fault, complete) =>
            {
                next(ArrSeg("asdf"), () =>
                    next(ArrSeg("jkl;"), () =>
                        next(ArrSeg("lol"), () =>
                            fault(
                                new Exception("ack!")))));
                return () => { };
            });

            Exception e = null;
            var result = SynchronousReadToEndOrException(ref e);
            Assert.IsNotNull(e);
            Assert.AreEqual("ack!", e);
            Assert.AreEqual("asdfjkl;lol", result);
        }
    }
}