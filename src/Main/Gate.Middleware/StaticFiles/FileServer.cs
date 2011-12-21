using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gate.Middleware.Utils;
using Gate.Owin;

namespace Gate.Middleware.StaticFiles
{
    public class FileServer
    {
        private const string OK = "200 OK";
        private const string PartialContent = "206 Partial Content";
        private const string NotFound = "404 Not Found";
        private const string Forbidden = "403 Forbidden";
        private const string RequestedRangeNotSatisfiable = "416 Requested Range Not Satisfiable";

        private readonly string root;
        private string pathInfo;
        private Tuple<long, long> range;

        // Note: Path should be exposed when implementing Sendfile middleware.
        private string path;

        public FileServer(string root)
        {
            this.root = root;
        }

        public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
        {
            pathInfo = env["owin.RequestPath"].ToString();

            if (pathInfo.StartsWith("/"))
            {
                pathInfo = pathInfo.Substring(1);
            }

            if (pathInfo.Contains(".."))
            {
                Fail(Forbidden, "Forbidden").Invoke(env, result, fault);
                return;
            }

            path = Path.Combine(root ?? string.Empty, pathInfo);

            if (!File.Exists(path))
            {
                Fail(NotFound, "File not found: " + pathInfo).Invoke(env, result, fault);
                return;
            }

            try
            {
                Serve(env).Invoke(env, result, fault);
            }
            catch (UnauthorizedAccessException)
            {
                Fail(Forbidden, "Forbidden").Invoke(env, result, fault);
            }
        }

        private static AppDelegate Fail(string status, string body, IDictionary<string, string> headers = null)
        {
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            return (env, res, err) =>
                res(
                    status,
                    new Dictionary<string, string>(headers)
                      {
                          {"Content-Type", "text/plain"},
                          {"Content-Length", body.Length.ToString()},
                          {"X-Cascade", "pass"}
                      },
                    TextBody.Create(body, Encoding.UTF8)
                  );
        }

        private AppDelegate Serve(IDictionary<string, object> environment)
        {
            var fileInfo = new FileInfo(path);
            var size = fileInfo.Length;

            string status;
            var headers = new Dictionary<string, string>
                              {
                                  {"Last-Modified", fileInfo.LastWriteTimeUtc.ToHttpDateString()},
                                  {"Content-Type", Mime.MimeType(fileInfo.Extension, "text/plain")}
                              };

            if (!RangeHeader.IsValid(environment))
            {
                status = OK;
                range = new Tuple<long, long>(0, size - 1);
            }
            else
            {
                var ranges = RangeHeader.Parse(environment, size);

                if (ranges == null)
                {
                    // Unsatisfiable.  Return error and file size.
                    return Fail(RequestedRangeNotSatisfiable, "Byte range unsatisfiable",
                                new Dictionary<string, string> { { "Content-Range", "bytes */" + size } });
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
                    headers["Content-Range"] = "bytes " + range.Item1 + "-" + range.Item2 + "/" + size;
                    size = range.Item2 - range.Item1 + 1;
                }
            }

            headers["Content-Length"] = size.ToString();

            return (env, res, err) =>
            {
                try
                {
                    res(status, headers, FileBody.Create(path, range));
                }
                catch (Exception ex)
                {
                    err(ex);
                }
            };
        }
    }
}