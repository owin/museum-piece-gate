using System;
using System.Collections.Generic;
using Ghost.Engine.Settings;

namespace Ghost.Engine.CommandLine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Arguments arguments;
            try
            {
                arguments = ParseArguments(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                ShowHelp(new string[0]);
                return;
            }
            if (arguments.ShowHelp)
            {
                ShowHelp(arguments.HelpArgs);
                return;
            }

            var engine = BuildEngine();
            using (StartServer(engine, arguments))
            {
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }
        private static Arguments ParseArguments(IEnumerable<string> args)
        {
            var parser = new Parser();
            return parser.Parse(args);
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

        private static void ShowHelp(IEnumerable<string> helpArgs)
        {
            Console.Write(
@"Usage: Ghost [options] [<application>]
Runs <application> on an http server
Example: Ghost -p8080 HelloWorld.Startup

Options:
 /s, --server=TYPE      Load assembly named ""Gate.Hosts.TYPE.dll"" to determine
                        http server to use. TYPE defaults to HttpListener.
 /u, --url=URIPREFIX    May be used to set --scheme, --host, --port, and 
                        --path options with a combined URIPREFIX value.
                        Format is '<scheme>://<host>[:<port>]<path>/'.
 /S, --scheme=SCHEME    Determine which socket protocol server should bind with.
                        SCHEME may be 'http' or 'https'. Defaults to 'http'.
 /h, --host=NAME        Which host name or IP address to listen on. 
                        NAME defaults to '+' for all IP addresses.
 /p, --port=NUMBER      Which TCP port to listen on. NUMBER defaults to 8080.
 /P, --path=PATH        Determines the virtual directory to run use as the
                        base path for <application> requests. PATH must start 
                        with a '/'.

Environment Variables:
PORT                    Changes the default TCP port to listen on when 
                        both --port and --url options are not provided.
OWIN_SERVER             Changes the default server TYPE to use when
                        the --server option is not provided.

");
        }
    }
}
