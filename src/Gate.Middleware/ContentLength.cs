using System;
using System.Linq;
using System.Collections.Generic;
using Gate.Owin;

namespace Gate.Middleware
{
    using Response = Tuple<string, IDictionary<string, string>, BodyDelegate>;

    public static class ContentLengthExtensions
    {
        static string[] ExcludeStatuses = new[] { "204", "205" };

        public static IAppBuilder ContentLength(this IAppBuilder builder)
        {
            return builder.Transform((response, respond, fault) => {
                var status = response.Item1;
                var headers = response.Item2;
                var body = response.Item3;
                if (!ExcludeStatuses.Any(i => status.StartsWith(i)) &&
                    !headers.ContainsKey("content-length") &&
                    !headers.ContainsKey("transfer-encoding") &&
                    body != null)
                {
                    var buffer = new DataBuffer();

                    body((data, ack) => {
                        buffer.Add(data);
                        return false;
                    },
                    // XXX propagate error through fault callback via transform
                    fault,
                    () => {
                        headers["Content-Length"] = buffer.GetCount().ToString();
                        respond(Tuple.Create<string, IDictionary<string, string>, BodyDelegate>(status, headers, (onNext, onError, onComplete) => {
                            buffer.Each(d => onNext(new ArraySegment<byte>(d), null));
                            onComplete();
                            return null;
                        }));
                    });
                }
                else
                    respond(response);
            });
        }

        class DataBuffer
        {
            List<byte[]> buffer = new List<byte[]>();
    
            public int GetCount()
            {
                return buffer.Aggregate(0, (c, d) => c + d.Length);
            }
    
            public void Add(ArraySegment<byte> d)
            {
                // XXX probably a better idea to make a big-ish buffer (maybe 8k?)
                // and reallocate only when it fills up.
                byte[] b = new byte[d.Count];
                System.Buffer.BlockCopy(d.Array, d.Offset, b, 0, d.Count);
                buffer.Add(b);
            }
    
            public void Each(Action<byte[]> each)
            {
                buffer.ForEach(each);
            }
        }

    }
}

