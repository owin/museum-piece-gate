using System;
using System.Collections.Generic;
using System.Text;
using Gate.Owin;

namespace Gate.Hosts.Manos.Tests
{
    public static class Startup
    {
        public static void Custom(IAppBuilder builder)
        {
            builder.Use<AppDelegate>(App);
        }

        static AppDelegate App(AppDelegate arg)
        {
            return (env, result, fault) => result("200 OK",
                new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase) { { "Content-Type", new[] { "text/plain" } } },
                (write, flush, end, cancel) =>
                {
                    var bytes = Encoding.Default.GetBytes("This is a custom page");
                    write(new ArraySegment<byte>(bytes));
                    end(null);
                });
        }
    }
}
