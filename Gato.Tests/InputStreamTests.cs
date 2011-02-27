using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Gato.Tests
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

        void CreateStream(Func<IObserver<Tuple<ArraySegment<byte>, Action>>, Action> observable)
        {
            stream = new ObservableInputStream(new Observable<Tuple<ArraySegment<byte>, Action>>(observable));
        }

        [Test]
        public void PsCs()
        {
            CreateStream(o =>
            {
                o.OnNext(Tuple.Create(ArrSeg("asdf"), (Action)null));
                o.OnNext(Tuple.Create(ArrSeg("jkl;"), (Action)null));
                o.OnNext(Tuple.Create(ArrSeg("lol"), (Action)null));
                o.OnCompleted();
                return () => { };
            });

            Exception e = null;
            var result = SynchronousReadToEndOrException(ref e);
            Assert.IsNull(e);
            Assert.AreEqual("asdfjkl;lol", result);
        }

        [Test]
        public void PsCsException()
        {
            CreateStream(o =>
            {
                o.OnNext(Tuple.Create(ArrSeg("asdf"), (Action)null));
                o.OnNext(Tuple.Create(ArrSeg("jkl;"), (Action)null));
                o.OnNext(Tuple.Create(ArrSeg("lol"), (Action)null));
                o.OnError(new Exception("ack!"));
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
            CreateStream(o =>
            {
                o.OnNext(Tuple.Create(ArrSeg("asdf"), (Action)(() => {
                    o.OnNext(Tuple.Create(ArrSeg("jkl;"), (Action)(() => {
                        o.OnNext(Tuple.Create(ArrSeg("lol"), (Action)(() => {
                            o.OnCompleted();
                        })));
                    })));
                })));
                return () => { };
            });

            Exception e = null;
            var result = SynchronousReadToEndOrException(ref e);
            Assert.IsNull(e);
            Assert.AreEqual("asdfjkl;lol", result);
        }

        [Test]
        public void PaCsException()
        {
            CreateStream(o => {
                o.OnNext(Tuple.Create(ArrSeg("asdf"), (Action)(() => {
                    o.OnNext(Tuple.Create(ArrSeg("jkl;"), (Action)(() => {
                        o.OnNext(Tuple.Create(ArrSeg("lol"), (Action)(() => {
                            o.OnError(new Exception("ack!"));
                        })));
                    })));
                })));
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
