using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Gate.Hosts.Kayak;
using Kayak;
using Kayak.Http;
using System.Threading.Tasks;

[assembly: ServerFactory]
namespace Gate.Hosts.Kayak
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ServerFactory : Attribute
    {
        public static IDisposable Create(AppFunc app, int port, TextWriter output)
        {
            app = ExecutionContextPerRequest.Middleware(app);
            var endPoint = new IPEndPoint(IPAddress.Any, port);

            var schedulerDelegate = new NullSchedulerDelegate(output);
            var scheduler = KayakScheduler.Factory.Create(schedulerDelegate);

            var context = new Dictionary<string, object>
            {
                {"gate.Output", output},
            };
            var channel = new GateRequestDelegate(app, context);

            var server = KayakServer.Factory.CreateHttp(channel, scheduler);
            var listen = server.Listen(endPoint);

            var thread = new Thread(_ => scheduler.Start());
            thread.Start();

            return new Disposable(() =>
            {
                scheduler.Stop();
                thread.Join(5000);
                listen.Dispose();
                server.Dispose();
            });
        }
    }

    public class NullSchedulerDelegate : ISchedulerDelegate
    {
        readonly TextWriter _output;

        public NullSchedulerDelegate(TextWriter output)
        {
            _output = output;
        }

        public void OnException(IScheduler scheduler, Exception e)
        {
            _output.WriteLine("Unhandled exception: " + e.Message);
        }

        public void OnStop(IScheduler scheduler)
        {
        }
    }
}
