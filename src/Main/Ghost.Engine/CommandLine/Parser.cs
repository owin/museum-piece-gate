using System;
using System.Collections.Generic;

namespace Ghost.Engine.CommandLine
{
    public class Parser
    {
        public Arguments Parse(IEnumerable<string> args)
        {
            var parameters = new Arguments();

            Action<string> argStartup;
            Action<string> optServer;
            Action<string> optScheme;
            Action<string> optUrl;
            Action<string> optHost;
            Action<string> optPort;
            Action<string> optPath;
            Action<string> optHelp;

            Action<string> nextOption;
            Action<string> nextValue;
            Action<string> next;

            argStartup = value =>
            {
                if (value == null) return;
                parameters.Startup = value;
                argStartup = extra =>
                {
                    if (extra != null) throw new Exception("Too many startup arguments");
                };
                nextValue = argStartup;
            };

            optServer = value =>
            {
                if (value == null) throw new Exception("server expected");
                parameters.Server = value;
                optServer = _ => { throw new Exception("Too many server options"); };
                nextValue = argStartup;
            };

            optUrl = value =>
            {
                if (value == null) throw new Exception("url value expected");
                parameters.Url = value;
                optUrl = _ => { throw new Exception("Too many url options"); };
                nextValue = argStartup;
            };

            optScheme = value =>
            {
                if (value == null) throw new Exception("scheme value expected");
                parameters.Scheme = value;
                optScheme = _ => { throw new Exception("Too many scheme options"); };
                nextValue = argStartup;
            };

            optHost = value =>
            {
                if (value == null) throw new Exception("host value expected");
                parameters.Host = value;
                optHost = _ => { throw new Exception("Too many host options"); };
                nextValue = argStartup;
            };

            optPort = value =>
            {
                if (value == null) throw new Exception("port value expected");
                parameters.Port = Convert.ToInt32(value);
                optPort = _ => { throw new Exception("Too many port options"); };
                nextValue = argStartup;
            };

            optPath = value =>
            {
                if (value == null) throw new Exception("path value expected");
                parameters.Path = value;
                optPath = _ => { throw new Exception("Too many path options"); };
                nextValue = argStartup;
            };

            optHelp = value =>
            {
                if (value == null) throw new Exception("path value expected");
                parameters.ShowHelp = true;
                parameters.HelpArgs = new List<string>();
                optHelp = _ => { throw new Exception("Too many help options"); };
                next = helpArg =>
                {
                    if (helpArg == null) return;
                    parameters.HelpArgs.Add(helpArg);
                };
            };

            nextValue = argStartup;
            nextOption = value =>
            {
                switch (value)
                {
                    case "--server":
                        nextValue = optServer;
                        break;
                    case "--url":
                        nextValue = optUrl;
                        break;
                    case "--scheme":
                        nextValue = optScheme;
                        break;
                    case "--host":
                        nextValue = optHost;
                        break;
                    case "--port":
                        nextValue = optPort;
                        break;
                    case "--path":
                        nextValue = optPath;
                        break;
                    case "--help":
                        optHelp(value);
                        break;
                    default:
                        throw new Exception("Unknown option " + value);
                }
            };

            next = value =>
            {
                if (value != null && value.StartsWith("--"))
                {
                    nextOption(value);
                }
                else
                {
                    nextValue(value);
                }
            };

            foreach (var arg in Preprocess(args))
            {
                next(arg);
            }
            next(null);
            return parameters;
        }

        private static IEnumerable<string> Preprocess(IEnumerable<string> args)
        {
            var aliases = new Dictionary<char, string>
                              {
                             {'s', "--server"},
                             {'u', "--url"},
                             {'S', "--scheme"},
                             {'h', "--host"},
                             {'p', "--port"},
                             {'P', "--path"},
                             {'?', "--help"},
                         };

            foreach (var arg in args)
            {
                string alias;
                if (arg.Length >= 2 && (arg[0] == '-' || arg[0] == '/') && aliases.TryGetValue(arg[1], out alias))
                {
                    yield return alias;
                    if (arg.Length > 2)
                        yield return arg.Substring(2);
                    continue;
                }

                if (arg.Length >= 2 && arg.StartsWith("--"))
                {
                    var delimiter = arg.IndexOf('=');
                    if (delimiter != -1)
                    {
                        yield return arg.Substring(0, delimiter);
                        yield return arg.Substring(delimiter + 1);
                        continue;
                    }
                }

                yield return arg;
            }
        }
    }

}
