using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gate.Middleware.Utils;
using Owin;
using System.Text;
using Gate.Utils;
using Gate.Middleware;

namespace Owin
{
    public static class ChunkedExtensions
    {
        public static IAppBuilder UseChunked(this IAppBuilder builder)
        {
            return builder.UseType<Chunked>();
        }
    }
}

namespace Gate.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // This middleware implements chunked response body encoding as the default encoding 
    // if the application does not specify Content-Length or Transfer-Encoding.
    public class Chunked
    {
        private readonly AppFunc nextApp;

        public Chunked(AppFunc nextApp)
        {
            this.nextApp = nextApp;
        }

        static readonly ArraySegment<byte> EndOfChunk = new ArraySegment<byte>(Encoding.ASCII.GetBytes("\r\n"));
        static readonly ArraySegment<byte> FinalChunk = new ArraySegment<byte>(Encoding.ASCII.GetBytes("0\r\n\r\n"));
        static readonly byte[] Hex = Encoding.ASCII.GetBytes("0123456789abcdef\r\n");

        public Task Invoke(IDictionary<string, object> env)
        {
            Response response = new Response(env);
            Stream orriginalStream = response.OutputStream;
            TriggerStream triggerStream = new TriggerStream(orriginalStream);
            response.OutputStream = triggerStream;
            FilterStream filterStream = null;
            triggerStream.OnFirstWrite = () =>
            {
                if (IsStatusWithNoNoEntityBody(response.StatusCode)
                    || response.Headers.ContainsKey("Content-Length")
                    || response.Headers.ContainsKey("Transfer-Encoding"))
                {
                    return;
                }

                // Buffer
                filterStream = new FilterStream(orriginalStream, OnWriteFilter);
                triggerStream.InnerStream = filterStream;
            };

            env[OwinConstants.ResponseBody] = triggerStream;

            return nextApp(env).Then(() =>
            {
                if (filterStream != null)
                {
                    // Write the chunked terminator
                    return filterStream.WriteAsync(FinalChunk.Array, FinalChunk.Offset, FinalChunk.Count);
                }
                
                return TaskHelpers.Completed();
            });
        }

        public static ArraySegment<byte>[] OnWriteFilter(ArraySegment<byte> data)
        {
            return data.Count == 0
                ? new[]
                {
                    data
                }
                : new[]
                {
                    ChunkPrefix((uint) data.Count), 
                    data, 
                    EndOfChunk,
                };
        }

        public static ArraySegment<byte> ChunkPrefix(uint dataCount)
        {
            var prefixBytes = new[]
            {
                Hex[(dataCount >> 28) & 0xf],
                Hex[(dataCount >> 24) & 0xf],
                Hex[(dataCount >> 20) & 0xf],
                Hex[(dataCount >> 16) & 0xf],
                Hex[(dataCount >> 12) & 0xf],
                Hex[(dataCount >> 8) & 0xf],
                Hex[(dataCount >> 4) & 0xf],
                Hex[(dataCount >> 0) & 0xf],
                Hex[16],
                Hex[17],
            };
            var shift = (dataCount & 0xffff0000) == 0 ? 16 : 0;
            shift += ((dataCount << shift) & 0xff000000) == 0 ? 8 : 0;
            shift += ((dataCount << shift) & 0xf0000000) == 0 ? 4 : 0;
            return new ArraySegment<byte>(prefixBytes, shift / 4, 10 - shift / 4);
        }

        private static bool IsStatusWithNoNoEntityBody(int status)
        {
            return (status >= 100 && status < 200) ||
                status == 204 ||
                status == 205 ||
                status == 304;
        }
    }
}

