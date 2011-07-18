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
        public static IServer CreateGate(this IServerFactory factory, string configurationString, IScheduler scheduler)
        {
            var app = AppBuilder.BuildConfiguration(configurationString);

            if (app == null)
                throw new Exception("Could not load Gate configuration from configuration string '" + configurationString + "'");

            return KayakServer.Factory.CreateHttp(new GateRequestDelegate(app), scheduler);
        }
    }
}