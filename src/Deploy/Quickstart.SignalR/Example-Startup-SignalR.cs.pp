using System.Threading;
using Owin;
using SignalR.Hosting.Owin;
using SignalR.Hubs;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Pipeline_030_SignalR(IAppBuilder builder)
        {
            builder.UseSignalR();
        }
    }

	// this is not a working demo, just a hub w/out static files at the moment

	public class MouseTracking : Hub
    {
        private static long _id;

        public void Join()
        {
            Caller.id = Interlocked.Increment(ref _id);
        }

        public void Move(int x, int y)
        {
            Clients.moveMouse(Caller.id, x, y);
        }
    }
}
