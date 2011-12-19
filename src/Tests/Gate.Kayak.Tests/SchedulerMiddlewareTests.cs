using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Kayak;
using Kayak.Http;

namespace Gate.Kayak.Tests
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
        static Func<int, int, int> _workaround;

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

            var app = new StaticApp(null, null, (onNext, onError, onComplete) =>
            {
                onNext(bytes("kanye "), null);
                onNext(bytes("west "), null);
                onNext(bytes("is "), null);
                onNext(bytes("a "), null);
                onNext(bytes("pussy."), null);
                return () => { };
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

        [Test]
        public void Wrapped_apps_onNext_gets_rescheduled_continuation_iff_wrapping_app_provides_continuation()
        {
            var app = new StaticApp(null, null, null);

            bool gotComplete = false;
            int gotOnNext = 0;
            Action ack0 = null;
            Action ack1 = null;

            var scheduler = new MockScheduler();

            app.OnRequest = () => {
                var env = new Environment(app.Env);

                env.BodyAction((data, ack) => {
                    gotOnNext++;

                    if (gotOnNext == 1)
                        ack0 = ack;
                    if (gotOnNext == 2)
                        ack1 = ack;

                    if (ack == null)
                        return false;
                    else
                    {
                        return true;
                    }
                },
                e => {
                },
                () => { gotComplete = true; });
            };

            var middleware = new RescheduleCallbacksMiddleware(app.Invoke, scheduler);

            scheduler.Post(() =>
            {
                middleware.Invoke(new Environment() {
                    BodyAction = (onNext, onError, onComplete) => {
                        onNext(default(ArraySegment<byte>), null);
                        onNext(default(ArraySegment<byte>), () => onComplete());
                        return () => { };
                    }
                }, (status, headers, body) =>
                {

                },
                e => { });
            });

            scheduler.Start();

            Assert.That(gotOnNext, Is.EqualTo(2));
            Assert.That(ack0, Is.Null);
            Assert.That(ack1, Is.Not.Null);

            // queues the continuation (which invokes onComplete) on the scheduler. onComplete should not be called yet.
            ack1();
            Assert.That(gotComplete, Is.False);

            // now run the scheduler more. after its done onComplete should have been called.
            scheduler.Start();
            Assert.That(gotComplete, Is.True);
        }
    }
}
