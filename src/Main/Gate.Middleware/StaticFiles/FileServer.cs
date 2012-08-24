using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Gate.Middleware.Utils;
using Owin;
using System.Threading.Tasks;

namespace Gate.Middleware.StaticFiles
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // Used by the Static middleware to send static files to the client.
    public class FileServer
    {
        private const int OK = 200;
        private const int PartialContent = 206;
        private const int NotFound = 404;
        private const int Forbidden = 403;
        private const int RequestedRangeNotSatisfiable = 416;

        private readonly string root;
        private string pathInfo;
        private Tuple<long, long> range;

        // Note: Path should be exposed when implementing Sendfile middleware.
        private string path;

        public FileServer(string root)
        {
            this.root = root;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            pathInfo = env.Get<string>(OwinConstants.RequestPath);

            if (pathInfo.StartsWith("/"))
            {
                pathInfo = pathInfo.Substring(1);
            }

            if (pathInfo.Contains(".."))
            {
                return Fail(Forbidden, "Forbidden").Invoke(env);
            }

            path = Path.Combine(root ?? string.Empty, pathInfo);

            if (!File.Exists(path))
            {
                return Fail(NotFound, "File not found: " + pathInfo).Invoke(env);
            }

            try
            {
                return Serve(env);
            }
            catch (UnauthorizedAccessException)
            {
                return Fail(Forbidden, "Forbidden").Invoke(env);
            }
        }

        private static AppFunc Fail(int status, string body, string headerName = null, string headerValue = null)
        {
            return env =>
                {
                    Response response = new Response(env);
                    response.StatusCode = status;
                    response.Headers
                        .SetHeader("Content-Type", "text/plain")
                        .SetHeader("Content-Length", body.Length.ToString(CultureInfo.InvariantCulture))
                        .SetHeader("X-Cascade", "pass");

                    if (headerName != null && headerValue != null)
                    {
                        response.Headers.SetHeader(headerName, headerValue);
                    }

                    response.Write(body);
                    return response.EndAsync();
                };
        }

        private Task Serve(IDictionary<string, object> env)
        {
            Request request = new Request(env);
            Response response = new Response(env);

            var fileInfo = new FileInfo(path);
            var size = fileInfo.Length;

            if (!RangeHeader.IsValid(request.Headers))
            {
                response.StatusCode = OK;
                range = new Tuple<long, long>(0, size - 1);
            }
            else
            {
                var ranges = RangeHeader.Parse(request.Headers, size);

                if (ranges == null)
                {
                    // Unsatisfiable.  Return error and file size.
                    return Fail(
                        RequestedRangeNotSatisfiable,
                        "Byte range unsatisfiable",
                        "Content-Range", "bytes */" + size)
                        .Invoke(env);
                }

                if (ranges.Count() > 1)
                {
                    // TODO: Support multiple byte ranges.
                    response.StatusCode = OK;
                    range = new Tuple<long, long>(0, size - 1);
                }
                else
                {
                    // Partial content
                    range = ranges.First();
                    response.StatusCode = PartialContent;
                    response.Headers.SetHeader("Content-Range", "bytes " + range.Item1 + "-" + range.Item2 + "/" + size);
                    size = range.Item2 - range.Item1 + 1;
                }
            }

            response.Headers
                .SetHeader("Last-Modified", fileInfo.LastWriteTimeUtc.ToHttpDateString())
                .SetHeader("Content-Type", Mime.MimeType(fileInfo.Extension, "text/plain"))
                .SetHeader("Content-Length", size.ToString(CultureInfo.InvariantCulture));

            return new FileBody(path, range).Start(response.OutputStream);
        }
    }
}