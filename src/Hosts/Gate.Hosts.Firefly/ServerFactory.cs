using System;
using Owin;

[assembly: Gate.Hosts.Firefly.ServerFactory]

namespace Gate.Hosts.Firefly
{
    public class ServerFactory : Attribute
    {
        public IDisposable Create(AppDelegate app, int port)
        {
            var serverFactory = new global::Firefly.Http.ServerFactory();
            return serverFactory.Create(app, port);
        }
    }
}
