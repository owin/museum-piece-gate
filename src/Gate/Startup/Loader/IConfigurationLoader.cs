using System;

namespace Gate.Startup.Loader
{
    public interface IConfigurationLoader
    {
        Action<AppBuilder> Load(string configurationString);
    }
}