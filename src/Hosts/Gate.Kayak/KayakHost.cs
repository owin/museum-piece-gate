using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kayak;
using Kayak.Http;
using System.Diagnostics;
using System.Net;
using Gate.Startup;

namespace Gate.Kayak
{
    public class KayakHost
    {
        public void Run(IPEndPoint listenEndPoint, AppDelegate app)
        {
            var server = KayakServer.Factory.CreateHttp(new RequestDelegate(app));
            server.Listen(listenEndPoint);
        }
    }
}