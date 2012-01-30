﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using Gate.Owin;
using Kayak;
using Kayak.Http;

namespace Gate.Hosts.Kayak
{
    class GateRequestDelegate : IHttpRequestDelegate
    {
        AppDelegate appDelegate;
        IDictionary<string, object> context;

        public GateRequestDelegate(AppDelegate appDelegate, IDictionary<string, object> context)
        {
            this.appDelegate = appDelegate;
            this.context = context;
        }

        public void OnRequest(HttpRequestHead head, IDataProducer body, IHttpResponseDelegate response)
        {
            var env = new Dictionary<string, object>();
            var request = new RequestEnvironment(env);

            if (context != null)
                foreach (var kv in context)
                    env[kv.Key] = kv.Value;

            if (head.Headers == null)
                request.Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
            else
                request.Headers = head.Headers.ToDictionary(kv => kv.Key, kv => (IEnumerable<string>)new[] { kv.Value }, StringComparer.OrdinalIgnoreCase);

            request.Method = head.Method ?? "";
            request.Path = head.Path ?? "";
            request.PathBase = "";
            request.QueryString = head.QueryString ?? "";
            request.Scheme = "http"; // XXX
            request.Version = "1.0";

            if (body == null)
                request.BodyDelegate = null;
            else
                request.BodyDelegate = (write, flush, end, cancellationToken) =>
                {
                    var d = body.Connect(new DataConsumer(
                        (data, continuation) => write(data) && continuation != null && flush(continuation),
                        ex => end(ex),
                        () => end(null)));
                    cancellationToken.Register(d.Dispose);
                };

            appDelegate(env, HandleResponse(response), HandleError(response));
        }

        ResultDelegate HandleResponse(IHttpResponseDelegate response)
        {
            return (status, headers, body) =>
            {
                if (headers == null)
                    headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

                if (body != null &&
                    !headers.ContainsKey("Content-Length") &&
                    !headers.ContainsKey("Transfer-Encoding"))
                {
                    // consume body and calculate Content-Length
                    BufferBody(response)(status, headers, body);
                }
                else
                {
                    response.OnResponse(new HttpResponseHead()
                    {
                        Status = status,
                        Headers = headers.ToDictionary(kv => kv.Key, kv => string.Join("\r\n", kv.Value.ToArray()), StringComparer.OrdinalIgnoreCase),
                    }, body == null ? null : new DataProducer(body));
                }
            };
        }

        ResultDelegate BufferBody(IHttpResponseDelegate response)
        {
            return (status, headers, body) =>
            {
                var buffer = new LinkedList<ArraySegment<byte>>();

                body(
                    data =>
                {
                    var copy = new byte[data.Count];
                    Buffer.BlockCopy(data.Array, data.Offset, copy, 0, data.Count);
                    buffer.AddLast(new ArraySegment<byte>(copy));
                    return false;
                },
                _=>false,
                error =>
                {
                    var contentLength = buffer.Aggregate(0, (r, i) => r + i.Count);

                    IDataProducer responseBody = null;

                    if (contentLength > 0)
                    {
                        headers["Content-Length"] = new[] {contentLength.ToString()};

                        responseBody = new DataProducer((write, flush, end, cancellationToken) =>
                        {
                            while (!cancellationToken.IsCancellationRequested && buffer.Count > 0)
                            {
                                var next = buffer.First;
                                buffer.RemoveFirst();
                                write(next.Value);
                            }

                            end(null);

                            buffer = null;
                        });
                    }

                    response.OnResponse(new HttpResponseHead()
                    {
                        Status = status,
                        Headers = headers.ToDictionary(kv => kv.Key, kv => string.Join("\r\n", kv.Value.ToArray()), StringComparer.OrdinalIgnoreCase),
                    }, responseBody);
                },
                CancellationToken.None);
            };
        }

        Action<Exception> HandleError(IHttpResponseDelegate response)
        {
            return error =>
            {
                Console.Error.WriteLine("Error from Gate application.");
                Console.Error.WriteStackTrace(error);

                response.OnResponse(new HttpResponseHead()
                {
                    Status = "503 Internal Server Error",
                    Headers = new Dictionary<string, string>()
                    {
                        { "Connection", "close" }
                    }
                }, null);
            };
        }
    }
}
