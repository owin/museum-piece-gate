using System;
using System.Collections.Generic;
using System.Text;
using Gate.Helpers.Utils;
using Gate.Utils;

namespace Gate
{
    public class Request : Environment
    {
        static readonly char[] CommaSemicolon = new[] {',', ';'};

        public Request(IDictionary<string, object> env) : base(env)
        {
        }

        public IDictionary<string, string> Query
        {
            get
            {
                var text = QueryString;
                if (Get<string>("Gate.Helpers.Request.Query:text") != text ||
                    Get<IDictionary<string, string>>("Gate.Helpers.Request.Query") == null)
                {
                    Env["Gate.Helpers.Request.Query:text"] = text;
                    Env["Gate.Helpers.Request.Query"] = ParamDictionary.Parse(text);
                }
                return Get<IDictionary<string, string>>("Gate.Helpers.Request.Query");
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
                string value;
                return (Headers != null && Headers.TryGetValue("Content-Type", out value)) ? value : null;
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


        public IDictionary<string, string> Post
        {
            get
            {
                if (HasFormData || HasParseableData)
                {
                    var input = Body;
                    if (input == null)
                    {
                        throw new InvalidOperationException("Missing input");
                    }

                    if (!ReferenceEquals(Get<object>("Gate.Helpers.Request.Post:input"), input) ||
                        Get<IDictionary<string, string>>("Gate.Helpers.Request.Post") == null)
                    {
                        var text = input.ToText(Encoding.UTF8);
                        Env["Gate.Helpers.Request.Post:input"] = input;
                        Env["Gate.Helpers.Request.Post:text"] = text;
                        Env["Gate.Helpers.Request.Post"] = ParamDictionary.Parse(text);
                    }
                    return Get<IDictionary<string, string>>("Gate.Helpers.Request.Post");
                }

                return ParamDictionary.Parse("");
            }
        }

        public string HostWithPort
        {
            get
            {
                string hostHeader;
                if (Headers != null &&
                    Headers.TryGetValue("Host", out hostHeader) &&
                        !string.IsNullOrWhiteSpace(hostHeader))
                {
                    return hostHeader;
                }

                var serverName = Get<string>("server.SERVER_NAME");
                if (string.IsNullOrWhiteSpace(serverName))
                    serverName = Get<string>("server.SERVER_ADDRESS");
                var serverPort = Get<string>("server.SERVER_PORT");

                return serverName + ":" + serverPort;
            }
        }

        public string Host
        {
            get
            {
                string hostHeader;
                if (Headers != null &&
                    Headers.TryGetValue("Host", out hostHeader) &&
                        !string.IsNullOrWhiteSpace(hostHeader))
                {
                    var delimiter = hostHeader.IndexOf(':');
                    return delimiter < 0 ? hostHeader : hostHeader.Substring(0, delimiter);
                }
                var serverName = Get<string>("server.SERVER_NAME");
                if (string.IsNullOrWhiteSpace(serverName))
                    serverName = Get<string>("server.SERVER_ADDRESS");
                return serverName;
            }
        }
    }
}