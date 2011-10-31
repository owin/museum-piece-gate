using System;
using System.Configuration;
using Gate.Builder;

namespace Gate.AspNet
{
    public static class AppHandlerSingleton
    {
        static AppHandlerSingleton()
        {
            SetFactory(DefaultFactory);
        }

        static Func<AppHandler> _accessor;

        public static AppHandler Instance
        {
            get { return _accessor(); }
            set { _accessor = () => value; }
        }

        public static void SetFactory(Func<AppHandler> factory)
        {
            var sync = new object();
            AppHandler instance = null;
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

        public static AppHandler DefaultFactory()
        {
            var configurationString = ConfigurationManager.AppSettings["Gate.Startup"];
            var app = AppBuilder.BuildConfiguration(configurationString);
            return new AppHandler(app);
        }
    }
}
