using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Owin;

namespace Gate.Middleware
{
    using Response = Tuple<string, IDictionary<string, string[]>, BodyDelegate>;

    public static class ContentLength
    {
        public static IAppBuilder UseContentLength(this IAppBuilder builder)
        {
            return builder.Use<AppDelegate>(Middleware);
        }


        public static AppDelegate Middleware(AppDelegate app)
        {
            return
                (call, callback) =>
                    app(
                        call,
                        (result, error) =>
                        {
                            if (error != null ||
                                IsStatusWithNoNoEntityBody(result.Status) ||
                                result.Headers.ContainsKey("Content-Length") ||
                                result.Headers.ContainsKey("Transfer-Encoding"))
                            {
                                callback(result, error);
                            }
                            else
                            {
                                var buffer = new DataBuffer();
                                result.Body.Invoke(
                                    buffer.Add,
                                    ex =>
                                    {
                                        buffer.End(ex);
                                        result.Headers.SetHeader("Content-Length", buffer.GetCount().ToString());
                                        result.Body = buffer.Body;
                                        callback(result, null);
                                    },
                                    call.CallDisposed);
                            }
                        });
        }



        private static bool IsStatusWithNoNoEntityBody(int status)
        {
            return (status >= 100 && status < 200) ||
                status == 204 ||
                status == 205 ||
                status == 304;
        }

        class DataBuffer
        {
            readonly List<ArraySegment<byte>> _buffers = new List<ArraySegment<byte>>();
            ArraySegment<byte> _tail = new ArraySegment<byte>(new byte[2048], 0, 0);
            Exception _error;

            public int GetCount()
            {
                return _buffers.Aggregate(0, (c, d) => c + d.Count);
            }

            public TempEnum Add(ArraySegment<byte> data, Action<Exception> continuation)
            {
                var remaining = data;
                while (remaining.Count != 0)
                {
                    if (_tail.Count + _tail.Offset == _tail.Array.Length)
                    {
                        _buffers.Add(_tail);
                        _tail = new ArraySegment<byte>(new byte[4096], 0, 0);
                    }
                    var copyCount = Math.Min(remaining.Count, _tail.Array.Length - _tail.Offset - _tail.Count);
                    Array.Copy(remaining.Array, remaining.Offset, _tail.Array, _tail.Offset + _tail.Count, copyCount);
                    _tail = new ArraySegment<byte>(_tail.Array, _tail.Offset, _tail.Count + copyCount);
                    remaining = new ArraySegment<byte>(remaining.Array, remaining.Offset + copyCount, remaining.Count - copyCount);
                }
                return OwinConstants.CompletedSynchronously;
            }

            public void End(Exception error)
            {
                _buffers.Add(_tail);
                _error = error;
            }

            public void Body(
                Func<ArraySegment<byte>, Action<Exception>, TempEnum> write,
                Action<Exception> end,
                CancellationToken cancel)
            {
                try
                {
                    foreach (var data in _buffers)
                    {
                        if (cancel.IsCancellationRequested)
                            break;

                        write(data, null);
                    }
                    end(_error);
                }
                catch (Exception ex)
                {
                    end(ex);
                }
            }
        }

    }
}

