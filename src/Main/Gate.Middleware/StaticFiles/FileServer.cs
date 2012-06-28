using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gate.Middleware.Utils;
using Owin;

namespace Gate.Middleware.StaticFiles
{
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

        public void Invoke(CallParameters call, Action<ResultParameters, Exception> callback)
        {
            pathInfo = call.Environment[OwinConstants.RequestPath].ToString();

            if (pathInfo.StartsWith("/"))
            {
                pathInfo = pathInfo.Substring(1);
            }

            if (pathInfo.Contains(".."))
            {
                Fail(Forbidden, "Forbidden").Invoke(call, callback);
                return;
            }

            path = Path.Combine(root ?? string.Empty, pathInfo);

            if (!File.Exists(path))
            {
                Fail(NotFound, "File not found: " + pathInfo).Invoke(call, callback);
                return;
            }

            try
            {
                Serve(call, callback);
            }
            catch (UnauthorizedAccessException)
            {
                Fail(Forbidden, "Forbidden").Invoke(call, callback);
            }
        }

        private static AppDelegate Fail(int status, string body, IDictionary<string, string[]> headers = null)
        {
            return (call, callback) =>
                callback(new ResultParameters
                {
                    Status = status,
                    Headers = Headers.New(headers)
                        .SetHeader("Content-Type", "text/plain")
                        .SetHeader("Content-Length", body.Length.ToString())
                        .SetHeader("X-Cascade", "pass"),
                    Body = TextBody.Create(body, Encoding.UTF8)
                }, null);
        }

        private void Serve(CallParameters call, Action<ResultParameters, Exception> callback)
        {
            var fileInfo = new FileInfo(path);
            var size = fileInfo.Length;

            int status;
            var headers = Headers.New()
                .SetHeader("Last-Modified", fileInfo.LastWriteTimeUtc.ToHttpDateString())
                .SetHeader("Content-Type", Mime.MimeType(fileInfo.Extension, "text/plain"));

            if (!RangeHeader.IsValid(call.Headers))
            {
                status = OK;
                range = new Tuple<long, long>(0, size - 1);
            }
            else
            {
                var ranges = RangeHeader.Parse(call.Headers, size);

                if (ranges == null)
                {
                    // Unsatisfiable.  Return error and file size.
                    Fail(
                        RequestedRangeNotSatisfiable,
                        "Byte range unsatisfiable",
                        Headers.New().SetHeader("Content-Range", "bytes */" + size))
                        .Invoke(call, callback);
                }

                if (ranges.Count() > 1)
                {
                    // TODO: Support multiple byte ranges.
                    status = OK;
                    range = new Tuple<long, long>(0, size - 1);
                }
                else
                {
                    // Partial content
                    range = ranges.First();
                    status = PartialContent;
                    headers.SetHeader("Content-Range", "bytes " + range.Item1 + "-" + range.Item2 + "/" + size);
                    size = range.Item2 - range.Item1 + 1;
                }
            }

            headers.SetHeader("Content-Length", size.ToString());

            try
            {
                callback(new ResultParameters
                {
                    Status = status,
                    Headers = headers,
                    Body = FileBody.Create(path, range)
                }, null);
            }
            catch (Exception ex)
            {
                callback(default(ResultParameters), ex);
            }
        }
    }
}