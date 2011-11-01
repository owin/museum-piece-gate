using System;

namespace Gate.Builder.Loader
{
    public interface IStartupLoader
    {
        Action<IAppBuilder> Load(string startupName);
    }
}