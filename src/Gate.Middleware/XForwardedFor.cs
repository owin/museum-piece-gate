using System;
using System.Linq;
using System.Collections.Generic;

namespace Gate
{
	public static class XForwardedFor
    {
        static readonly string[] empty = new[] { "" };

        static IEnumerable<string> GetRemoteHosts(this IDictionary<string, object> env)
        {
            var headers = (env as Gate.Environment ?? new Gate.Environment(env)).Headers;

            if (headers == null)
                return empty;

            var xff = headers.ContainsKey("x-forwarded-for") ? headers["x-forwarded-for"] : null;

            if (string.IsNullOrWhiteSpace(xff))
                return empty;

            return xff.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        public static string GetRemoteAddress(this IDictionary<string, object> env)
        {
            return env.GetRemoteHosts().LastOrDefault();
        }

        public static IEnumerable<string> GetRemoteAddresses(this IDictionary<string, object> env)
        {
            return env.GetRemoteHosts().Reverse().Skip(1).Reverse();
        }

        public static IEnumerable<string> GetRemoteAddresses(this IDictionary<string, object> env, params string[] knownProxies)
        {
            return env.GetRemoteAddresses().Reverse().SkipWhile(s => knownProxies.Contains(s)).Reverse();
        }
	}
}
