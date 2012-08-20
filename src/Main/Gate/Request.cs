using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gate.Utils;
using Owin;

namespace Gate
{
    // A helper class for creating, modifying, or consuming CallParameters request data.
    public class Request
    {
        CallParameters _call;
        static readonly char[] CommaSemicolon = new[] { ',', ';' };

        public Request()
        {
            _call = new CallParameters()
            {
                Body = null,
                Environment = new Dictionary<string, object>(),
                Headers = Gate.Headers.New()
            };
        }

        public Request(CallParameters call)
        {
            _call = call;
        }

        public IDictionary<string, object> Environment
        {
            get { return _call.Environment; }
            set { _call.Environment = value; }
        }
        public IDictionary<string, string[]> Headers
        {
            get { return _call.Headers; }
            set { _call.Headers = value; }
        }
        public Stream Body
        {
            get { return _call.Body; }
            set { _call.Body = value; }
        }
        public Task Completed
        {
            get { return Get<Task>(OwinConstants.CallCompleted); }
            set { Environment[OwinConstants.CallCompleted] = value; }
        }

        private T Get<T>(string name)
        {
            object value;
            return Environment.TryGetValue(name, out value) ? (T)value : default(T);
        }

        /// <summary>
        /// "owin.Version" The string "1.0" indicating OWIN version 1.0. 
        /// </summary>
        public string Version
        {
            get { return Get<string>(OwinConstants.Version); }
            set { Environment[OwinConstants.Version] = value; }
        }

        /// <summary>
        /// "owin.RequestProtocol" A string containing the protocol name and version (e.g. "HTTP/1.0" or "HTTP/1.1"). 
        /// </summary>
        public string Protocol
        {
            get { return Get<string>(OwinConstants.RequestProtocol); }
            set { Environment[OwinConstants.RequestProtocol] = value; }
        }

        /// <summary>
        /// "owin.RequestMethod" A string containing the HTTP request method of the request (e.g., "GET", "POST"). 
        /// </summary>
        public string Method
        {
            get { return Get<string>(OwinConstants.RequestMethod); }
            set { Environment[OwinConstants.RequestMethod] = value; }
        }

        /// <summary>
        /// "owin.RequestScheme" A string containing the URI scheme used for the request (e.g., "http", "https").  
        /// </summary>
        public string Scheme
        {
            get { return Get<string>(OwinConstants.RequestScheme); }
            set { Environment[OwinConstants.RequestScheme] = value; }
        }

        /// <summary>
        /// "owin.RequestPathBase" A string containing the portion of the request path corresponding to the "root" of the application delegate. The value may be an empty string.  
        /// </summary>
        public string PathBase
        {
            get { return Get<string>(OwinConstants.RequestPathBase); }
            set { Environment[OwinConstants.RequestPathBase] = value; }
        }

        /// <summary>
        /// "owin.RequestPath" A string containing the request path. The path must be relative to the "root" of the application delegate. 
        /// </summary>
        public string Path
        {
            get { return Get<string>(OwinConstants.RequestPath); }
            set { Environment[OwinConstants.RequestPath] = value; }
        }

        /// <summary>
        /// "owin.QueryString" A string containing the query string component of the HTTP request URI (e.g., "foo=bar&baz=quux"). The value may be an empty string.
        /// </summary>
        public string QueryString
        {
            get { return Get<string>(OwinConstants.RequestQueryString); }
            set { Environment[OwinConstants.RequestQueryString] = value; }
        }


        /// <summary>
        /// "host.TraceOutput" A TextWriter that directs trace or logger output to an appropriate place for the host
        /// </summary>
        public TextWriter TraceOutput
        {
            get { return Get<TextWriter>(OwinConstants.TraceOutput); }
            set { Environment[OwinConstants.TraceOutput] = value; }
        }

        public IDictionary<string, string> Query
        {
            get
            {
                var text = QueryString;
                if (Get<string>("Gate.Request.Query#text") != text ||
                    Get<IDictionary<string, string>>("Gate.Request.Query") == null)
                {
                    Environment["Gate.Request.Query#text"] = text;
                    Environment["Gate.Request.Query"] = ParamDictionary.Parse(text);
                }
                return Get<IDictionary<string, string>>("Gate.Request.Query");
            }
        }

        static readonly char[] CookieParamSeparators = new[] { ';', ',' };
        public IDictionary<string, string> Cookies
        {
            get
            {
                var cookies = Get<IDictionary<string, string>>("Gate.Request.Cookies#dictionary");
                if (cookies == null)
                {
                    cookies = new Dictionary<string, string>(StringComparer.Ordinal);
                    Environment["Gate.Request.Cookies#dictionary"] = cookies;
                }

                var text = Headers.GetHeader("Cookie");
                if (Get<string>("Gate.Request.Cookies#text") != text)
                {
                    cookies.Clear();
                    foreach (var kv in ParamDictionary.ParseToEnumerable(text, CookieParamSeparators))
                    {
                        if (!cookies.ContainsKey(kv.Key))
                            cookies.Add(kv);
                    }
                    Environment["Gate.Request.Cookies#text"] = text;
                }
                return cookies;
            }
        }

        public bool HasFormData
        {
            get
            {
                var mediaType = MediaType;
                return (Method == "POST" && string.IsNullOrEmpty(mediaType))
                    || mediaType == "application/x-www-form-urlencoded"
                        || mediaType == "multipart/form-data";
            }
        }

        public bool HasParseableData
        {
            get
            {
                var mediaType = MediaType;
                return mediaType == "application/x-www-form-urlencoded"
                    || mediaType == "multipart/form-data";
            }
        }


        public string ContentType
        {
            get
            {
                return Headers.GetHeader("Content-Type");
            }
        }

        public string MediaType
        {
            get
            {
                var contentType = ContentType;
                if (contentType == null)
                    return null;
                var delimiterPos = contentType.IndexOfAny(CommaSemicolon);
                return delimiterPos < 0 ? contentType : contentType.Substring(0, delimiterPos);
            }
        }

        public Task CopyToStreamAsync(Stream stream)
        {
            if (_call.Body == null)
            {
                return TaskHelpers.Completed();
            }
            return _call.Body.CopyToAsync(stream);
        }

        public Task<string> ReadTextAsync()
        {
            var text = Get<string>("Gate.Request.Text");

            var thisInput = Body;
            var lastInput = Get<object>("Gate.Request.Text#input");

            if (text != null && ReferenceEquals(thisInput, lastInput))
            {
                return TaskHelpers.FromResult(text);
            }

            var buffer = new MemoryStream();

            //TODO: determine encoding from request content type
            return CopyToStreamAsync(buffer)
                .Then(() =>
                {
                    text = new StreamReader(buffer).ReadToEnd();
                    Environment["Gate.Request.Text#input"] = thisInput;
                    Environment["Gate.Request.Text"] = text;
                    return text;
                });
        }

        public string ReadText()
        {
            return ReadTextAsync().Result;
        }

        public Task<IDictionary<string, string>> ReadFormAsync()
        {
            if (!HasFormData && !HasParseableData)
            {
                return TaskHelpers.FromResult(ParamDictionary.Parse(""));
            }

            var form = Get<IDictionary<string, string>>("Gate.Request.Form");
            var thisInput = Body;
            var lastInput = Get<object>("Gate.Request.Form#input");
            if (form != null && ReferenceEquals(thisInput, lastInput))
            {
                return TaskHelpers.FromResult(form);
            }

            return ReadTextAsync().Then(text =>
            {
                form = ParamDictionary.Parse(text);
                Environment["Gate.Request.Form#input"] = thisInput;
                Environment["Gate.Request.Form"] = form;
                return form;
            });
        }

        public IDictionary<string, string> ReadForm()
        {
            return ReadFormAsync().Result;
        }


        public string HostWithPort
        {
            get
            {
                var hostHeader = Headers.GetHeader("Host");
                if (!string.IsNullOrWhiteSpace(hostHeader))
                {
                    return hostHeader;
                }

                return string.Empty;
            }
            set
            {
                Headers.SetHeader("Host", value);
            }
        }

        public string Host
        {
            get
            {
                var hostHeader = Headers.GetHeader("Host");
                if (!string.IsNullOrWhiteSpace(hostHeader))
                {
                    var delimiter = hostHeader.IndexOf(':');
                    return delimiter < 0 ? hostHeader : hostHeader.Substring(0, delimiter);
                }

                return string.Empty;
            }
            set
            {
                string port = Port;
                if (string.IsNullOrWhiteSpace(value))
                {
                    Headers.Remove("Host");
                }
                else if (!string.IsNullOrWhiteSpace(port))
                {
                    Headers.SetHeader("Host", value + ":" + port);
                }
                else
                {
                    Headers.SetHeader("Host", value);
                }
            }
        }

        public string Port
        {
            get
            {
                var hostHeader = Headers.GetHeader("Host");
                if (!string.IsNullOrWhiteSpace(hostHeader))
                {
                    var delimiter = hostHeader.IndexOf(':');
                    return delimiter < 0 ? string.Empty : hostHeader.Substring(delimiter + 1);
                }

                return string.Empty;
            }
            set
            {
                string host = Host;
                if (string.IsNullOrWhiteSpace(value))
                {
                    Host = host; // Truncate port
                }
                else
                {
                    Headers.SetHeader("Host", host + ":" + value);
                }
            }
        }

        public CallParameters Call
        {
            get { return _call; }
        }
    }
}