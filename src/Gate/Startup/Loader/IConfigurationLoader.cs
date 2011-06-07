using System;

namespace Gate.Startup
{
    public interface IConfigurationLoader
    {
        Action<IAppBuilder> Load(string configurationString);
    }
}