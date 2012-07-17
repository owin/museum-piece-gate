using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Owin;

namespace Gate.Middleware
{
    using System.IO;
    using System.Threading.Tasks;

    public static class ContentLength
    {
        public static IAppBuilder UseContentLength(this IAppBuilder builder)
        {
            return builder.Use<AppDelegate>(Middleware);
        }
        
        public static AppDelegate Middleware(AppDelegate app)
        {
            return call =>
            {
                TaskCompletionSource<ResultParameters> tcs = new TaskCompletionSource<ResultParameters>();
                app(call).ContinueWith( 
                    appTask =>
                    {
                        if (tcs.SetIfTaskFailed(appTask))
                        {
                            return;
                        }

                        ResultParameters result = appTask.Result;

                        if (IsStatusWithNoNoEntityBody(result.Status)
                            || result.Headers.ContainsKey("Content-Length") 
                            || result.Headers.ContainsKey("Transfer-Encoding"))
                        {
                            tcs.TrySetResult(result);
                            return;
                        }

                        if (result.Body == null)
                        {
                            result.Headers.SetHeader("Content-Length", "0");
                            tcs.TrySetResult(result);
                            return;
                        }

                        // Buffer the body
                        MemoryStream buffer = new MemoryStream();
                        result.Body(buffer, call.Completed).ContinueWith(
                            bodyTask =>
                            {
                                if (tcs.SetIfTaskFailed(bodyTask))
                                {
                                    return;
                                }

                                buffer.Seek(0, SeekOrigin.Begin);
                                result.Headers.SetHeader("Content-Length", buffer.Length.ToString());
                                result.Body = (stream, cancel) => // TODO: Async delegate
                                {
                                    buffer.CopyTo(stream);
                                    return TaskHelpers.Completed();
                                };

                                tcs.TrySetResult(result);
                            }, call.Completed);

                    }, call.Completed);
                return tcs.Task;
            };
        }

        private static bool IsStatusWithNoNoEntityBody(int status)
        {
            return (status >= 100 && status < 200) ||
                status == 204 ||
                status == 205 ||
                status == 304;
        }
    }
}

