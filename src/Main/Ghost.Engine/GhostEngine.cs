﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Owin;
using Ghost.Engine.Settings;

namespace Ghost.Engine
{
    public class GhostEngine : IGhostEngine
    {
        private readonly IGhostSettings _settings;

        public GhostEngine(IGhostSettings settings)
        {
            _settings = settings;
        }

        public IDisposable Start(StartInfo info)
        {
            ResolveOutput(info);
            ResolveServerFactory(info);
            ResolveApp(info);
            ResolveUrl(info);
            return StartServer(info);
        }

        void ResolveOutput(StartInfo info)
        {
            if (info.Output != null) return;

            if (!string.IsNullOrWhiteSpace(info.OutputFile))
            {
                info.Output = new StreamWriter(info.OutputFile, true);
            }
            else
            {
                info.Output = _settings.DefaultOutput;
            }
        }

        private void ResolveServerFactory(StartInfo info)
        {
            if (info.ServerFactory != null) return;

            var serverName = info.Server ?? _settings.DefaultServer;

            // TODO: error message for server assembly not found
            var serverAssembly = Assembly.Load(_settings.ServerAssemblyPrefix + serverName);

            // TODO: error message for assembly does not have ServerFactory attribute
            info.ServerFactory = serverAssembly.GetCustomAttributes(false)
                .Cast<Attribute>()
                .Single(x => x.GetType().Name == "ServerFactory");
        }


        private void ResolveApp(StartInfo info)
        {
            if (info.App != null) return;

            var startup = _settings.Loader.Load(info.Startup);
            info.App = _settings.Builder.Build<AppDelegate>(startup);
        }

        private void ResolveUrl(StartInfo info)
        {
            if (info.Url != null) return;
            info.Scheme = info.Scheme ?? _settings.DefaultScheme;
            info.Host = info.Host ?? _settings.DefaultHost;
            info.Port = info.Port ?? _settings.DefaultPort;
            info.Path = info.Path ?? "";
            if (info.Path != "" && !info.Path.StartsWith("/"))
            {
                info.Path = "/" + info.Path;
            }
            if (info.Port.HasValue)
            {
                info.Url = info.Scheme + "://" + info.Host + ":" + info.Port + info.Path + "/";
            }
            else
            {
                info.Url = info.Scheme + "://" + info.Host + info.Path + "/";
            }
        }

        private static IDisposable StartServer(StartInfo info)
        {
            var serverFactoryMethod = info.ServerFactory.GetType().GetMethod("Create");
            var serverFactoryParameters = serverFactoryMethod.GetParameters()
                .Select(parameterInfo => SelectParameter(parameterInfo, info))
                .ToArray();
            return (IDisposable)serverFactoryMethod.Invoke(info.ServerFactory, serverFactoryParameters.ToArray());
        }


        private static object SelectParameter(ParameterInfo parameterInfo, StartInfo info)
        {
            switch (parameterInfo.Name)
            {
                case "url":
                    return info.Url;
                case "port":
                    return info.Port;
                case "app":
                    return info.App;
                case "host":
                    return info.Host;
                case "path":
                    return info.Path;
                case "output":
                    return info.Output;
            }
            return null;
        }
    }
}
