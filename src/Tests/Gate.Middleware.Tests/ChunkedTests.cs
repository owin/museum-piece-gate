using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin;
using Owin.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ChunkedTests
    {
        private ResultParameters Call(Action<IAppBuilder> pipe)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppDelegate)builder.Build(typeof(AppDelegate));
            return app(new Request().Call).Result;
        }

        private string ReadBody(Func<Stream, Task> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            using (MemoryStream buffer = new MemoryStream())
            {
                body(buffer).Wait();
                return Encoding.ASCII.GetString(buffer.ToArray());
            }
        }

        [Test]
        public void ChunkPrefixHasCorrectResults()
        {
            AssertChunkPrefix(Chunked.ChunkPrefix(1), "1\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(15), "f\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10), "10\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x80), "80\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0xff), "ff\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10), "10\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x100), "100\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x1000), "1000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10000), "10000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x100000), "100000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x1000000), "1000000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10000000), "10000000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0), "0\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0xffffffff), "ffffffff\r\n");
        }

        static void AssertChunkPrefix(ArraySegment<byte> data, string expected)
        {
            Assert.That(Encoding.ASCII.GetString(data.Array, data.Offset, data.Count), Is.EqualTo(expected));
        }

        [Test]
        public void Does_not_encode_if_content_length_present()
        {
            var response = Call(b => b
                .UseChunked()
                .UseDirect((req, resp) =>
                {
                    resp.SetHeader("Content-Length", "12");
                    resp.SetHeader("Content-Type", "text/plain");
                    resp.StartAsync().Then(
                        resp1 =>
                        {
                            resp1.Write("hello ");
                            resp1.Write("world.");
                            resp1.End();
                        })
                        .Catch(errorInfo =>
                        {
                            return errorInfo.Handled();
                        });

                    return resp.ResultTask;
                }));

            Assert.That(response.Headers.ContainsKey("transfer-encoding"), Is.False);
            Assert.That(ReadBody(response.Body), Is.EqualTo("hello world."));
        }

        [Test]
        public void Does_not_encode_if_transfer_encoding_is_present()
        {
            var response = Call(b => b
                .UseChunked()
                .UseDirect((req, resp) =>
                {
                    resp.SetHeader("transfer-encoding", "girl");
                    resp.SetHeader("Content-Type", "text/plain");
                    resp.StartAsync().Then(
                        resp1 =>
                        {
                            resp1.Write("hello ");
                            resp1.Write("world.");
                            resp1.End();
                        });

                    return resp.ResultTask;
                }));

            Assert.That(response.Headers.GetHeader("Transfer-Encoding"), Is.EqualTo("girl"));
            Assert.That(ReadBody(response.Body), Is.EqualTo("hello world."));
        }

        [Test]
        public void Encodes_if_content_length_is_not_present()
        {
            var response = Call(b => b
                .UseChunked()
                .UseDirect((req, resp) =>
                {
                    resp.SetHeader("Content-Type", "text/plain");
                    resp.StartAsync().Then(
                        resp1 =>
                        {
                            resp1.Write("hello ");
                            resp1.Write("world.");
                            resp1.End();
                        });

                    return resp.ResultTask;
                }));

            Assert.That(response.Headers.GetHeader("Transfer-Encoding"), Is.EqualTo("chunked"));
            Assert.That(ReadBody(response.Body), Is.EqualTo("6\r\nhello \r\n6\r\nworld.\r\n0\r\n\r\n"));
        }
    }
}
