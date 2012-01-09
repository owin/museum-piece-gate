using System;
using System.Net;
using Kayak;

namespace $rootnamespace$
{
    public class KayakStarter : ISchedulerDelegate
    {
        public static void Start(int port, bool debug)
        {
            Gate.Hosts.Kayak.KayakGate.Start(
                new KayakStarter(),
                new IPEndPoint(IPAddress.Any, port),
                debug ? "$rootnamespace$.Startup.Debug" : "$rootnamespace$.Startup");
        }

        public void OnException(IScheduler scheduler, Exception e)
        {
        }

        public void OnStop(IScheduler scheduler)
        {
        }
    }
}
