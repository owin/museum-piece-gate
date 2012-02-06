﻿using System.Collections.Generic;
using Owin;
using Kayak;
using Kayak.Http;
using System.Net;

namespace Gate.Hosts.Kayak
{
    public static class KayakGate
    {
        public static void Start(ISchedulerDelegate schedulerDelegate, IPEndPoint listenEP, AppDelegate app)
        {
            Start(schedulerDelegate, listenEP, app, null);
        }

        public static void Start(ISchedulerDelegate schedulerDelegate, IPEndPoint listenEP, AppDelegate app, IDictionary<string, object> context)
        {
            var scheduler = KayakScheduler.Factory.Create(schedulerDelegate);
            var server = KayakServer.Factory.CreateGate(app, scheduler, context);
            
			using (server.Listen(listenEP))
            	scheduler.Start();
        }
    }

    public static class HttpServerExtensions
    {
        public static IServer CreateGate(this IServerFactory factory, AppDelegate app, IScheduler scheduler)
        {
            return CreateGate(factory, app, scheduler, null);
        }

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
