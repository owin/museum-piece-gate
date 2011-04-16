using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Startup.Tests
{
    public class Startup
    {
        public static void Configuration(AppBuilder builder)
        {
            ++ConfigurationCalls;
        }

        public static int ConfigurationCalls { get; set; }
    }
}