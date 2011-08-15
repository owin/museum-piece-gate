using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Kayak;
using Kayak.Http;

namespace Gate.Kayak
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
            var env = new Environment();

            if (context != null)
                foreach (var kv in context)
                    env[kv.Key] = kv.Value;

            env.Headers = head.Headers ?? new Dictionary<string, string>();
            env.Method = head.Method ?? "";
            env.Path = head.Path ?? "";
            env.PathBase = "";
            env.QueryString = head.QueryString ?? "";
            env.Scheme = "http"; // XXX
            env.Version = "1.0";
            
            if (body == null)
                env.Body = null;
            else
                env.Body = (onData, onError, onEnd) =>
                {
                    var d = body.Connect(new DataConsumer(onData, onError, onEnd));
                    return () => { if (d != null) d.Dispose(); };
                };

            appDelegate(env, HandleResponse(response), HandleError(response));
        }

        ResultDelegate HandleResponse(IHttpResponseDelegate response)
        {
            return (status, headers, body) =>
            {
                if (headers == null)
                    headers = new Dictionary<string, string>();

                if (body != null &&
                    !headers.ContainsKey("Content-Length") &&
                    !(headers.ContainsKey("Transfer-Encoding") && headers["Transfer-Encoding"] == "chunked"))
                {
                    // consume body and calculate Content-Length
                    BufferBody(response)(status, headers, body);
                }
                else
                {
                    response.OnResponse(new HttpResponseHead()
                    {
                        Status = status,
                        Headers = headers
                    }, body == null ? null : new DataProducer(body));
                }
            };
        }

        ResultDelegate BufferBody(IHttpResponseDelegate response)
        {
            return (status, headers, body) =>
            {
                var buffer = new LinkedList<ArraySegment<byte>>();

                body((data, continuation) =>
                {
                    var copy = new byte[data.Count];
                    Buffer.BlockCopy(data.Array, data.Offset, copy, 0, data.Count);
                    buffer.AddLast(new ArraySegment<byte>(copy));
                    return false;
                },
                HandleError(response),
                () =>
                {
                    var contentLength = buffer.Aggregate(0, (r, i) => r + i.Count);

                    IDataProducer responseBody = null;

                    if (contentLength > 0)
                    {
                        headers["Content-Length"] = contentLength.ToString();
                        responseBody = new DataProducer((onData, onError, onComplete) =>
                        {
                            bool cancelled = false;

                            while (!cancelled && buffer.Count > 0)
                            {
                                var next = buffer.First;
                                buffer.RemoveFirst();
                                onData(next.Value, null);
                            }

                            onComplete();

                            buffer = null;

                            return () => cancelled = true;
                        });
                    }

                    response.OnResponse(new HttpResponseHead()
                    {
                        Status = status,
                        Headers = headers
                    }, responseBody);
                });
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
