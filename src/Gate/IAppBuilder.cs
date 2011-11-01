using System;
using Gate.Owin;

namespace Gate
{
    public interface IAppBuilder
    {
        IAppBuilder Use(Func<AppDelegate, AppDelegate> factory);
        AppDelegate Build();
    }
}
