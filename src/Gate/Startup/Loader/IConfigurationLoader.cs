using System;

namespace Gate
{
    public interface IConfigurationLoader
    {
        Action<IAppBuilder> Load(string configurationString);
    }
}