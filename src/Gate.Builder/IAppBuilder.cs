using System;
using Gate.Owin;

namespace Gate.Builder
{
    public interface IAppBuilder
    {
        IAppBuilder Use(Func<AppDelegate, AppDelegate> factory);
        AppDelegate Build();
    }
}
