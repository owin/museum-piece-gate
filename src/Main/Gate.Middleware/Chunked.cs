using System;
using System.Collections.Generic;
using Gate.Owin;
using System.Text;

namespace Gate.Middleware
{
    public static class ChunkedExtensions
    {
        public static IAppBuilder Chunked(this IAppBuilder builder)
        {
            return builder.Transform((r, cb, ex) =>
            {
                var headers = r.Item2;
                var body = r.Item3;

                if (!headers.HasHeader("Content-Length") &&
                    (!headers.HasHeader("Transfer-Encoding") || headers.GetHeader("Transfer-Encoding") == "chunked"))
                {
                    headers.SetHeader("Transfer-Encoding", "chunked");
                    body = (onNext, onError, onComplete) =>
                    {
                        var chunked = new ChunkedBody();
                        return r.Item3(
                            (data, ack) => onNext(chunked.EncodeChunk(data), ack),
                            onError,
                            () =>
                            {
                                onNext(ChunkedBody.TerminalChunk, null);
                                onComplete();
                            });
                    };
                }

                cb(Tuple.Create(r.Item1, headers, body));
            });
        }

        class ChunkedBody
        {
            public static ArraySegment<byte> TerminalChunk = new ArraySegment<byte>(Encoding.ASCII.GetBytes("0\r\n\r\n"));
            public static byte[] CRLF = Encoding.ASCII.GetBytes("\r\n");

            public ArraySegment<byte> EncodeChunk(ArraySegment<byte> data)
            {
                var lengthString = data.Count.ToString("x");
                var chunk = new byte[data.Count + lengthString.Length + 4];

                var length = Encoding.ASCII.GetBytes(lengthString + "\r\n");
                var position = 0;
                Buffer.BlockCopy(length, 0, chunk, position, length.Length);
                position += length.Length;

                Buffer.BlockCopy(data.Array, 0, chunk, position, data.Count);
                position += data.Count;

                Buffer.BlockCopy(CRLF, 0, chunk, position, CRLF.Length);

                return new ArraySegment<byte>(chunk);
            }
        }
    }
}

