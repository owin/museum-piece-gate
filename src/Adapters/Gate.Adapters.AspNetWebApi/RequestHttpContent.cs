using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Gate.Adapters.AspNetWebApi
{
    class RequestHttpContent : HttpContent
    {
        readonly BodyDelegate _body;
        readonly CancellationToken _cancellationToken;

        public RequestHttpContent(BodyDelegate body, CancellationToken cancellationToken)
        {
            _body = body;
            _cancellationToken = cancellationToken;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var tcs = new TaskCompletionSource<object>();
            _body.Invoke(
                data =>
                {
                    stream.Write(data.Array, data.Offset, data.Count);
                    return false;
                },
                _ =>
                {
                    stream.Flush();
                    return false;
                },
                ex =>
                {
                    if (ex == null)
                    {
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        tcs.TrySetException(ex);
                    }
                },
                _cancellationToken);
            return tcs.Task;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}