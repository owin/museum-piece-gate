using System;

namespace Gate.Startup.Loader
{
    public interface IConfigurationLoader
    {
        Action<IAppBuilder> Load(string configurationString);
    }
}