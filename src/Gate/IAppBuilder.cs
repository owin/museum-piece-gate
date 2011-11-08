using System;
using Gate.Owin;

namespace Gate
{
    public interface IAppBuilder
    {
        IAppBuilder Use<TApp>(Func<TApp, TApp> middleware);
        TApp Build<TApp>(Action<IAppBuilder> fork);
    }
}
