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
				res.Write("Hello");
				res.End();
			});
        }
    }
}
