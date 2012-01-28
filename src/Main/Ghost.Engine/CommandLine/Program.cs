using System;
using System.Collections.Generic;
using System.Linq;
using Ghost.Engine.Settings;
using NDesk.Options;

namespace Ghost.Engine.CommandLine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var arguments = ParseArguments(args);
            if (arguments == null)
            {
                return;
            }

            var engine = BuildEngine();
            using (StartServer(engine, arguments))
            {
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }

        private static IGhostEngine BuildEngine()
        {
            var settings = new GhostSettings();
            TakeDefaultsFromEnvironment(settings);
            return new GhostEngine(settings);
        }

        private static IDisposable StartServer(IGhostEngine engine, Arguments arguments)
        {
            return engine.Start(new StartInfo
            {
                Server = arguments.Server,
                Startup = arguments.Startup,
                Url = arguments.Url,
                Scheme = arguments.Scheme,
                Host = arguments.Host,
                Port = arguments.Port,
                Path = arguments.Path,
            });
        }

        private static Arguments ParseArguments(IEnumerable<string> args)
        {
            var arguments = new Arguments();
            var optionSet = new OptionSet()
                .Add(
                    "s=|server=", 
                    @"Load assembly named ""Gate.Hosts.TYPE.dll"" to determine http server to use. TYPE defaults to HttpListener.", 
                    x => arguments.Server = x)
                .Add(
                    "u=|url=", 
                    @"May be used to set --scheme, --host, --port, and --path options with a combined URIPREFIX value. Format is '<scheme>://<host>[:<port>]<path>/'.", 
                    x => arguments.Url = x)
                .Add(
                    "S=|scheme=", 
                    @"Determine which socket protocol server should bind with. SCHEME may be 'http' or 'https'. Defaults to 'http'.", 
                    x => arguments.Scheme = x)
                .Add(
                    "h=|host=", 
                    @"Which host name or IP address to listen on. NAME defaults to '+' for all IP addresses.", 
                    x => arguments.Host = x)
                .Add(
                    "p=|port=", 
                    @"Which TCP port to listen on. NUMBER defaults to 8080.", 
                    (int x) => arguments.Port = x)
                .Add(
                    "P=|path=", 
                    @"Determines the virtual directory to run use as the base path for <application> requests. PATH must start with a '/'.", 
                    x => arguments.Path = x)
                .Add(
                    "?|help", 
                    @"Show this message and exit.", 
                    x => arguments.ShowHelp = x != null)
                ;

            List<string> extra;
            try
            {
                extra = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Ghost: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'Ghost --help' for more information.");
                return null;
            }
            if (arguments.ShowHelp)
            {
                ShowHelp(optionSet, extra);
                return null;
            }
            arguments.Startup = string.Join(" ", extra.ToArray());
            return arguments;
        }

        private static void ShowHelp(OptionSet optionSet, IEnumerable<string> helpArgs)
        {
            Console.Write(
@"Usage: Ghost [options] [<application>]
Runs <application> on an http server
Example: Ghost -p8080 HelloWorld.Startup

Options:
");
            optionSet.WriteOptionDescriptions(Console.Out);
            Console.Write(
@"
Environment Variables:
PORT                         Changes the default TCP port to listen on when 
                               both --port and --url options are not provided.
OWIN_SERVER                  Changes the default server TYPE to use when
                               the --server option is not provided.

");
        }

        private static void TakeDefaultsFromEnvironment(GhostSettings settings)
        {
            var port = Environment.GetEnvironmentVariable("PORT", EnvironmentVariableTarget.Process);
            int portNumber;
            if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port, out portNumber))
                settings.DefaultPort = portNumber;

            var owinServer = Environment.GetEnvironmentVariable("OWIN_SERVER", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(owinServer))
                settings.DefaultServer = owinServer;
        }
    }
}
