using System;

namespace Gate.Startup
{
    public interface IAppBuilder
    {
        IAppBuilder Use(Func<AppDelegate, AppDelegate> factory);
        AppDelegate Build();
    }
}
