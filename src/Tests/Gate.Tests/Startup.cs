using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Startup;

namespace Gate.Tests
{
    public class Startup
    {
        public static void Configuration(IAppBuilder builder)
        {
            ++ConfigurationCalls;
        }

        public static int ConfigurationCalls { get; set; }
    }
}