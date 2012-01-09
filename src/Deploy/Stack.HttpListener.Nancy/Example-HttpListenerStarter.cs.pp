using System;
using System.Net;

namespace $rootnamespace$
{
    public class HttpListenerStarter
    {
        public static IDisposable Start(int port, bool debug)
        {
            return Gate.Hosts.HttpListener.Server.Create(
				debug ? "$rootnamespace$.Startup.Debug" : "$rootnamespace$.Startup",
				port);
        }
    }
}
