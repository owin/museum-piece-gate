using System;
using System.Configuration;
using Gate.Builder;
using Gate.Owin;

namespace Gate.Hosts.Manos
{
    public static class Server
    {
        public static IDisposable Create(int port)
        {
            return Create(port, "");
        }

        public static IDisposable Create(int port, string path)
        {
            return Create(ConfigurationManager.AppSettings["Gate.Startup"], port, path);
        }

        public static IDisposable Create(string startupName, int port)
        {
            return Create(startupName, port, "");
        }

        public static IDisposable Create(string startupName, int port, string path)
        {
            AppDelegate app = AppBuilder.BuildConfiguration(startupName);
            return Create(app, port, path);
        }

        public static IDisposable Create(AppDelegate app, int port)
        {
            return Create(app, port, "");
        }
        public static IDisposable Create(AppDelegate app, int port, string path)
        {
            return new Disposable(() => { });
        }


        public class Disposable : IDisposable
        {
            readonly Action _dispose;

            public Disposable(Action dispose)
            {
                


                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }
}
