using System;
using System.Collections.Generic;
using System.Text;
using Owin;
using System.Threading.Tasks;

namespace Gate.Hosts.HttpListener.Tests
{
    public static class Startup
    {
        public static void Custom(IAppBuilder builder)
        {
            builder.Use<AppDelegate>(App);
        }

        static AppDelegate App(AppDelegate arg)
        {
            return call =>
            {
                ResultParameters result = new ResultParameters()
                {
                    Status = 200,
                    Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { "Content-Type", new[] { "text/plain" } } },
                    Properties = new Dictionary<string, object>(),
                    Body = stream =>
                    {
                        var bytes = Encoding.Default.GetBytes("This is a custom page");
                        stream.Write(bytes, 0, bytes.Length);

                        TaskCompletionSource<object> bodyTcs = new TaskCompletionSource<object>();
                        bodyTcs.TrySetResult(null);
                        return bodyTcs.Task;
                    }
                };

                TaskCompletionSource<ResultParameters> requestTcs = new TaskCompletionSource<ResultParameters>();
                requestTcs.TrySetResult(result);
                return requestTcs.Task;
            };
        }
    }
}
