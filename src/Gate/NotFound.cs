using System;
using System.Collections.Generic;
using System.Text;

namespace Gate
{
    public class NotFound
    {
        public static AppDelegate Create()
        {
            var body = new ArraySegment<byte>(Encoding.UTF8.GetBytes(@"
<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML 2.0//EN"">
<html><head>
<title>404 Not Found</title>
</head><body>
<h1>Not Found</h1>
<p>The requested URL was not found on this server.</p>
</body></html>
"));

            return (env, result, fault) =>
                result(
                    "404 Not Found",
                    new Dictionary<string, string> {{"Content-Type", "text/html"}},
                    (next, error, complete) =>
                    {
                        next(body, null);
                        complete();
                        return () => { };
                    });
        }
    }
}