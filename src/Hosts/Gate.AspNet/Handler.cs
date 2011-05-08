using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Helpers;

namespace Gate.AspNet
{
    public static class Handler
    {
        static AppDelegate _app = NotFound.Create();

        public static void Run(AppDelegate app)
        {
            _app = app;
        }

        public static AppDelegate Call
        {
            get { return _app; }
        }
    }
}