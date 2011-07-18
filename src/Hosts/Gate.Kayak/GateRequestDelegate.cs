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

        public GateRequestDelegate(AppDelegate appDelegate)
        {
            this.appDelegate = appDelegate;
        }

        public void OnRequest(HttpRequestHead head, IDataProducer body, IHttpResponseDelegate response)
        {
            var env = new Environment();
            env.Headers = head.Headers;
            env.Method = head.Method;
            env.Path = head.Uri;
            env.PathBase = "";
            env.QueryString = ""; // XXX
            env.Scheme = "http"; // XXX
            env.Version = "1.0";
            
            if (body != null)
                env.Body = (onData, onError, onEnd) =>
                {
                    var d = body.Connect(new DataConsumer(onData, onError, onEnd));
                    return () => d.Dispose();
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
                error =>
                {
                    HandleError(response)(error);
                },
                () =>
                {
                    headers["Content-Length"] = buffer.Aggregate(0, (r, i) => r + i.Count).ToString();

                    response.OnResponse(new HttpResponseHead()
                    {
                        Status = status,
                        Headers = headers
                    },
                    new DataProducer((onData, onError, onComplete) =>
                    {
                        bool cancelled = false;

                        while (!cancelled && buffer.Count > 0)
                        {
                            var next = buffer.First;
                            buffer.RemoveFirst();
                            onData(next.Value, null);
                        }

                        buffer = null;

                        return () => cancelled = true;
                    }));
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
