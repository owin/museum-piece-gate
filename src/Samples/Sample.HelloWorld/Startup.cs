using Gate;
using Owin;

namespace Sample.HelloWorld
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.RunInline((req, resp) =>
            {
                resp.Status = "200 OK";
                resp.ContentType = "text/html";

                resp.Write("<html>")
                    .Write("<head><title>Hello world</title></head>")
                    .Write("<body>")
                    .Write("<p>Hello world!</p>")
                    .Write("<ul>");

                foreach (var kv in req)
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
