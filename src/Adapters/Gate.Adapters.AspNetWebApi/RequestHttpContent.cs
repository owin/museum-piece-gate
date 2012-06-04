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
                (data, callback) =>
                {
                    if (data.Count == 0)
                    {
                        stream.Flush();
                    }
                    else if (callback == null)
                    {
                        stream.Write(data.Array, data.Offset, data.Count);
                    }
                    else
                    {
                        var sr = stream.BeginWrite(data.Array, data.Offset, data.Count, ar =>
                        {
                            if (!ar.CompletedSynchronously)
                            {
                                try { stream.EndWrite(ar); }
                                catch { }
                                try { callback.Invoke(); }
                                catch { }
                            }
                        }, null);

                        if (!sr.CompletedSynchronously)
                        {
                            return true;
                        }
                        stream.EndWrite(sr);
                        callback.Invoke();
                    }
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