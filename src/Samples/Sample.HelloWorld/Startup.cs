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

                resp.Write("<html>");
                resp.Write("<head><title>Hello world</title></head>");
                resp.Write("<body>");
                resp.Write("<p>Hello world!</p>");
                resp.Write("<ul>");

                resp.Write("<h3>Environment</h3>");
                foreach (var kv in req)
                {
                    if (kv.Value is IDictionary<string, string[]>)
                    {
                        resp.Write("<li>&laquo;");
                        resp.Write(kv.Key);
                        resp.Write("&raquo;<br/><ul>");
                        foreach (var kv2 in kv.Value as IDictionary<string, string[]>)
                        {
                            resp.Write("<li>&laquo;");
                            resp.Write(kv2.Key);
                            resp.Write("&raquo; = ");
                            resp.Write(string.Join(", ", kv2.Value.ToArray()));
                            resp.Write("</code></li>");
                        }
                        resp.Write("</ul></li>");
                    }
                    else
                    {
                        resp.Write("<li>&laquo;");
                        resp.Write(kv.Key);
                        resp.Write("&raquo;<br/><code>");
                        resp.Write(kv.Value.ToString());
                        resp.Write("</code></li>");
                    }
                }

                resp.Write("<h3>Cookies</h3>");
                foreach (var kv in req.Cookies)
                {
                    resp.Write("<li>&laquo;");
                    resp.Write(kv.Key);
                    resp.Write("&raquo;<br/><code>");
                    resp.Write(kv.Value.ToString());
                    resp.Write("</code></li>");
                }


                resp.Write("</ul>");
                resp.Write("</body>");
                resp.Write("</html>");
                resp.End();

            });
        }
    }
}
