using Nancy;

namespace Sample.AspNet
{
    public class MainModule : NancyModule
    {
        public MainModule()
        {
            Get["/"] = parameters => { return View["staticview.html"]; };
        }
    }
}