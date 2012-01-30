using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Kayak;
using Kayak.Http;

namespace Gate.Hosts.Kayak.Tests
{
    class MockScheduler : IScheduler
    {
        public List<Action> actions = new List<Action>();
        public void Post(Action action)
        {
            actions.Add(action);
        }

        public void Start()
        {
            while (actions.Count > 0)
            {
                var a = actions[0];
                actions.RemoveAt(0);
                a();
            }
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }

    [TestFixture]
    public class SchedulerMiddlewareTests
    {
#pragma warning disable 169
        static Func<int, int, int> _workaround;
#pragma warning restore 169

        [Test]
        public void Response_stream_is_rebuffered()
        {
            // strategy:
            // reuse a single buffer for all writes.
            // don't spool through the scheduler until after app is done writing.
            //
            // if implementation simply keeps a reference to the supplied buffer, 
            // its contents will be the contents of the final write and the
            // received body will be garbage. consumer must copy out of
            // producer's buffer.

            byte[] b = new byte[6];

            Func<string, ArraySegment<byte>> bytes = s =>
            {
                var sb = Encoding.ASCII.GetBytes(s);
                System.Buffer.BlockCopy(sb, 0, b, 0, sb.Length);
                return new ArraySegment<byte>(b, 0, sb.Length);
            };

            var app = new StaticApp(null, null, (write, flush, end, cancel) =>
            {
                write(bytes("kanye "));
                write(bytes("west "));
                write(bytes("is "));
                write(bytes("a "));
                write(bytes("pussy."));
                end(null);
            });

            var scheduler = new MockScheduler();
            var middleware = new RescheduleCallbacksMiddleware(app.Invoke, scheduler);
            var responseDelegate = new MockResponseDelegate();

            BufferingConsumer bodyConsumer = null;

            scheduler.Post(() =>
                {
                    middleware.Invoke(new Dictionary<string, object>(), (status, headers, body) =>
                        {
                            bodyConsumer = body.Consume();
                        }, e => { });
                });

            scheduler.Start();

            var bodyString = bodyConsumer.Buffer.GetString();

            Assert.That(bodyString, Is.EqualTo("kanye west is a pussy."));
        }


    }
}
