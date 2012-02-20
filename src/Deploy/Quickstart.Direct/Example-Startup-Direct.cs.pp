using Gate;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Cascade_070_Direct(IAppBuilder builder)
        {
            builder.MapDirect("/direct", (req,res) =>
            {
                res.ContentType = "text/plain";
                res.Write("Hello, ").Write(req.PathBase).Write(req.Path).Write("!");
                res.End();
            });
        }
    }
}
