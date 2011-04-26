using System;
using System.Collections.Generic;
using System.Text;
using Gate.Helpers.Utils;
using Gate.Utils;

namespace Gate
{
    public class Request : Environment
    {
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

        public IDictionary<string, string> Post
        {
            get
            {
                //TEST TEST TEST!!!
                var input = Body;
                if (input == null)
                {
                    throw new InvalidOperationException("Missing input");
                }

                if (!ReferenceEquals(Get<object>("Gate.Helpers.Request.Post:input"), input) ||
                    Get<IDictionary<string, string>>("Gate.Helpers.Request.Post") == null)
                {
                    Env["Gate.Helpers.Request.Post:input"] = input;
                    Env["Gate.Helpers.Request.Post"] = ParamDictionary.Parse(input.ToText(Encoding.UTF8));
                }
                return Get<IDictionary<string, string>>("Gate.Helpers.Request.Post");

                //if @env["rack.input"].nil?
                //       raise "Missing rack.input"
                //     elsif @env["rack.request.form_input"].eql? @env["rack.input"]
                //       @env["rack.request.form_hash"]
                //     elsif form_data? || parseable_data?
                //       @env["rack.request.form_input"] = @env["rack.input"]
                //       unless @env["rack.request.form_hash"] = parse_multipart(env)
                //         form_vars = @env["rack.input"].read

                //         # Fix for Safari Ajax postings that always append \0
                //         form_vars.sub!(/\0\z/, '')

                //         @env["rack.request.form_vars"] = form_vars
                //         @env["rack.request.form_hash"] = parse_query(form_vars)

                //         @env["rack.input"].rewind
                //       end
                //       @env["rack.request.form_hash"]
                //     else
                //       {}
                //     end
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
                return Get<string>("server.SERVER_NAME");
            }
        }
    }
}