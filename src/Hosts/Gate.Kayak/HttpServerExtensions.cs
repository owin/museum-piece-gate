using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kayak;
using Kayak.Http;
using System.Diagnostics;
using System.Net;

namespace Gate.Kayak
{
    public static class HttpServerExtensions
    {
        public static IServer CreateGate(this IServerFactory factory, AppDelegate app, IScheduler scheduler, IDictionary<string, object> context)
        {
            if (context == null)
                context = new Dictionary<string, object>();

            if (!context.ContainsKey("kayak.Scheduler"))
                context["kayak.Scheduler"] = scheduler;

            return factory.CreateHttp(new GateRequestDelegate(app, context), scheduler);
        }
    }
}
