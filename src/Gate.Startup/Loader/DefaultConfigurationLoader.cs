using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Startup.Loader
{
    public class DefaultConfigurationLoader : IConfigurationLoader
    {
        public Action<AppBuilder> Load(string configurationString)
        {
            return null;
        }
    }
}
