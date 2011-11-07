using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Owin;

namespace Gate.Middleware
{
    /// <summary>
    /// Sets content type in response if none present
    /// </summary>
    public static class ContentTypeExtensions
    {
        const string DefaultContentType = "text/html";

        public static IAppBuilder ContentType(this IAppBuilder builder)
        {
            return builder.ContentType(DefaultContentType);
        }

        public static IAppBuilder ContentType(this IAppBuilder builder, string contentType)
        {
            return builder.Use(a => Middleware(a, contentType));
        }

        static AppDelegate Middleware(AppDelegate app, string contentType)
        {
            return (env, result, fault) => app(
                env,
                (status, headers, body) =>
                {
                    if (!headers.Any(kv => string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)))
                        headers.Add("Content-Type", contentType);
                    result(status, headers, body);
                },
                fault);
        }
    }
}