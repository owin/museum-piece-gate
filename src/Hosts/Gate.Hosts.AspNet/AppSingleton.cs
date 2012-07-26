using System;
using System.Configuration;
using Gate.Builder;
using Owin;

namespace Gate.Hosts.AspNet
{
    public static class AppSingleton
    {
        static AppSingleton()
        {
            SetFactory(DefaultFactory);
        }

        static Func<AppDelegate> _accessor;

        public static AppDelegate Instance
        {
            get { return _accessor(); }
            set { _accessor = () => value; }
        }

        public static void SetFactory(Func<AppDelegate> factory)
        {
            var sync = new object();
            AppDelegate instance = null;
            _accessor = () =>
            {
                lock (sync)
                {
                    if (instance == null)
                    {
                        instance = factory();
                        _accessor = () => instance;
                    }
                    return instance;
                }
            };
        }

        public static AppDelegate DefaultFactory()
        {
            var configurationString = ConfigurationManager.AppSettings["Gate.Startup"];
            return AppBuilder.BuildPipeline<AppDelegate>(configurationString);
        }
    }
}
