using System;
using Owin;

[assembly: Gate.Hosts.Firefly.ServerFactory]

namespace Gate.Hosts.Firefly
{
    public class ServerFactory : Attribute
    {
        public static IDisposable Create(AppDelegate app, int port)
        {
            // TODO: Re-enable after Firefly is updated. app = ExecutionContextPerRequest.Middleware(app);
            var serverFactory = new global::Firefly.Http.ServerFactory();
            return serverFactory.Create(app, port);
        }
    }
}
