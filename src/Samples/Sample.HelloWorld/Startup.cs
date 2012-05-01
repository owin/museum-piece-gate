using System;
using System.Collections.Generic;
using System.Linq;
using Gate;
using Owin;

namespace Sample.HelloWorld
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.RunDirect((req, resp) =>
            {
                resp.Status = "200 OK";
                resp.ContentType = "text/html";
                resp.SetCookie("my-cookie", "my-value");
                resp.SetCookie("my-30-day-cookie", new Response.Cookie("hello-this-month") { Expires = DateTime.UtcNow.AddDays(30) });
                resp.SetCookie("last-path-cookie", new Response.Cookie("hello-path " + req.Path) { Path = req.PathBase + req.Path });

                resp.Write("<html>")
                    .Write("<head><title>Hello world</title></head>")
                    .Write("<body>")
                    .Write("<p>Hello world!</p>")
                    .Write("<ul>");

                resp.Write("<h3>Environment</h3>");
                foreach (var kv in req)
                {
                    if (kv.Value is IDictionary<string, IEnumerable<string>>)
                    {
                        resp.Write("<li>&laquo;")
                            .Write(kv.Key)
                            .Write("&raquo;<br/><ul>");
                        foreach (var kv2 in kv.Value as IDictionary<string, IEnumerable<string>>)
                        {
                            resp.Write("<li>&laquo;")
                                .Write(kv2.Key)
                                .Write("&raquo; = ")
                                .Write(string.Join(", ", kv2.Value.ToArray()))
                                .Write("</code></li>");
                        }
                        resp.Write("</ul></li>");
                    }
                    else
                    {
                        resp.Write("<li>&laquo;")
                            .Write(kv.Key)
                            .Write("&raquo;<br/><code>")
                            .Write(kv.Value.ToString())
                            .Write("</code></li>");
                    }
                }

                resp.Write("<h3>Cookies</h3>");
                foreach (var kv in req.Cookies)
                {
                    resp.Write("<li>&laquo;")
                        .Write(kv.Key)
                        .Write("&raquo;<br/><code>")
                        .Write(kv.Value.ToString())
                        .Write("</code></li>");
                }


                resp
                    .Write("</ul>")
                    .Write("</body>")
                    .Write("</html>")
                    .End();

            });
        }
    }
}
