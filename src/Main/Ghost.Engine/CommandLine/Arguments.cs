using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ghost.Engine.CommandLine
{
    public class Arguments
    {
        public string Server { get; set; }

        public string Startup { get; set; }

        public string Url { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Path { get; set; }

        public bool ShowHelp { get; set; }
        public IList<string> HelpArgs { get; set; }
    }
}
