using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Helpers
{
    /// <summary>
    /// Sets content type in response if none present
    /// </summary>
    public class ContentType
    {
        public static AppDelegate Create(AppDelegate app)
        {
            return Create(app, "text/html");
        }

        public static AppDelegate Create(AppDelegate app, string contentType)
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