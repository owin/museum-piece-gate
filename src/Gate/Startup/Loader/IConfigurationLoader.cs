using System;

namespace Gate.Startup
{
    public interface IConfigurationLoader
    {
        Action<AppBuilder> Load(string configurationString);
    }
}