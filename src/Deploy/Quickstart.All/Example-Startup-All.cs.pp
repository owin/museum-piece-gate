using System;
using System.Text;
using Gate;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Pipeline_010_All(IAppBuilder builder)
        {
            builder.Use(HomePage);
        }

		static AppDelegate HomePage(AppDelegate app)
		{
		    return (env, result, fault) =>
		    {
		        var path = (string)env["owin.RequestPath"];
                if (path != "/")
                {
                    app(env, result, fault);
                }
                else
                {
                    result(
                        "200 OK",
                        Headers.New().SetHeader("Content-Type", "text/html"),
                        (write, flush, end, cancel) =>
                        {
							var data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(HomePageText));
                            write(data);
                            end(null);
                        });
                }
		    };
		}

        const string HomePageText =
            @"
<html>
<head>
  <title>All Quickstarts</title>
</head>
<body>
  <ul>
    <li><a href=""/main"">AspNetWebApi</a></li>
    <li><a href=""/direct"">Direct</a></li>
    <li><a href=""/nancy"">Nancy</a></li>
	<li><span title=""this is not a working demo, just a hub w/out static files at the moment""><a href=""/signalr/hubs"">SignalR</a> (todo - need a demo)</span></li>
    <li><a href=""/wilson"">Wilson</a></li>
    <li><a href=""/wilsonasync"">WilsonAsync</a></li>	
  </ul>
</body>
</html>
";
    }
}
