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
            builder.UseDirect((req, resp) =>
            {
                resp.Status = "200 OK";
                resp.ContentType = "text/html";
                resp.SetCookie("my-cookie", "my-value");
                resp.SetCookie("my-30-day-cookie", new Response.Cookie("hello-this-month") { Expires = DateTime.UtcNow.AddDays(30) });
                resp.SetCookie("last-path-cookie", new Response.Cookie("hello-path " + req.Path) { Path = req.PathBase + req.Path });

                resp.Body.Write("<html>");
                resp.Body.Write("<head><title>Hello world</title></head>");
                resp.Body.Write("<body>");
                resp.Body.Write("<p>Hello world!</p>");
                resp.Body.Write("<ul>");

                resp.Body.Write("<h3>Environment</h3>");
                foreach (var kv in req.Environment)
                {
                    resp.Body.Write("<li>&laquo;");
                    resp.Body.Write(kv.Key);
                    resp.Body.Write("&raquo;<br/><code>");
                    resp.Body.Write(kv.Value.ToString());
                    resp.Body.Write("</code></li>");
                }

                resp.Body.Write("<h3>Headers</h3>");
                foreach (var kv2 in req.Headers)
                {
                    resp.Body.Write("<li>&laquo;");
                    resp.Body.Write(kv2.Key);
                    resp.Body.Write("&raquo; = ");
                    resp.Body.Write(string.Join(", ", kv2.Value.ToArray()));
                    resp.Body.Write("</code></li>");
                }

                resp.Body.Write("<h3>Cookies</h3>");
                foreach (var kv in req.Cookies)
                {
                    resp.Body.Write("<li>&laquo;");
                    resp.Body.Write(kv.Key);
                    resp.Body.Write("&raquo;<br/><code>");
                    resp.Body.Write(kv.Value.ToString());
                    resp.Body.Write("</code></li>");
                }
                
                // TODO: Request body?

                resp.Body.Write("</ul>");
                resp.Body.Write("</body>");
                resp.Body.Write("</html>");
                return resp.EndAsync();
            });
        }
    }
}
