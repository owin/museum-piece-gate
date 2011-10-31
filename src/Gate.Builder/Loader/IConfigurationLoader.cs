using System;

namespace Gate.Builder.Loader
{
    public interface IConfigurationLoader
    {
        Action<IAppBuilder> Load(string configurationString);
    }
}