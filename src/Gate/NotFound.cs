﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Gate
{
    public class NotFound : IApplication
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

        AppDelegate IApplication.Create()
        {
            return Create();
        }

        public static AppDelegate Create()
        {
            return Invoke;
        }

        public static void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
        {
            result(
                "404 Not Found",
                new Dictionary<string, string> {{"Content-Type", "text/html"}},
                (next, error, complete) =>
                {
                    next(Body, null);
                    complete();
                    return () => { };
                });
        }
    }
}