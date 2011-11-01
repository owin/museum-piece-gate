using System;
using Gate.Owin;

namespace Gate
{
    public interface IAppBuilder
    {
        IAppBuilder Use(Func<AppDelegate, AppDelegate> middleware);
        AppDelegate Build();
    }
}
