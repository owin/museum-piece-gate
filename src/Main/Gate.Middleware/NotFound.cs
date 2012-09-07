﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gate.Utils;
using Owin;

namespace Gate.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // Provides a default 404 Not Found HTML response.
    public static class NotFound
    {
        static readonly byte[] body = Encoding.UTF8.GetBytes(@"
<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML 2.0//EN"">
<html><head>
<title>404 Not Found</title>
</head><body>
<h1>Not Found</h1>
<p>The requested URL was not found on this server.</p>
</body></html>
");

        public static AppFunc App()
        {
            return Call;
        }

        public static Task Call(IDictionary<string, object> env)
        {
            return new Response(env)
            {
                StatusCode = 404, 
                ReasonPhrase = "Not Found", 
                ContentType = "text/html"
            }.WriteAsync(body);
        }
    }
}
