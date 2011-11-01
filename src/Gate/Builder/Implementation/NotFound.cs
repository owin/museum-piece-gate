using System;
using System.Collections.Generic;
using System.Text;
using Gate.Owin;

namespace Gate.Builder.Implementation
{
    static class NotFound
    {

        static readonly ArraySegment<byte> Body = new ArraySegment<byte>(Encoding.UTF8.GetBytes(@"
<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML 2.0//EN"">
<html><head>
<title>404 Not Found</title>
</head><body>
<h1>Not Found</h1>
<p>The requested URL was not found on this server.</p>
</body></html>
"));

        public static AppDelegate App()
        {
            return (env, result, fault) => result(
                "404 Not Found",
                new Dictionary<string, string> { { "Content-Type", "text/html" } },
                (next, error, complete) =>
                {
                    next(Body, null);
                    complete();
                    return () => { };
                });
        }
    }
}
