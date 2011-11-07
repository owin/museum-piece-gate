using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Gate.Helpers;
using Gate.Owin;
using Gate.Builder;

namespace Gate.TestHelpers
{
    public static class AppUtils
    {
        public static FakeHostResponse CallPipe(Action<IAppBuilder> pipe, Action<FakeHostRequest> request)
        {
            return new FakeHost(AppBuilder.BuildConfiguration(pipe)).Invoke(request);
        }

        public static FakeHostResponse CallPipe(Action<IAppBuilder> pipe)
        {
            return CallPipe(pipe, FakeHostRequest.GetRequest());
        }

        public static FakeHostResponse Call(AppDelegate app)
        {
            return Call(app, "");
        }

        public static FakeHostResponse Call(AppDelegate app, string path)
        {
            return new FakeHost(app).GET(path);
        }

        public static AppDelegate ShowEnvironment()
        {
            return (env, result, fault) =>
            {
                var response = new Response(result) {Status = "200 OK", ContentType = "text/xml"};
                response.Finish((error, complete) =>
                {
                    var detail = env.Select(kv => new XElement(kv.Key, kv.Value));
                    var xml = new XElement("xml", detail.OfType<object>().ToArray());
                    response.Write(xml.ToString());
                    complete();
                });
            };
        }

        public static IDictionary<string, string> CreateHeaderDictionary()
        {
            return new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public static IDictionary<string, string> CreateHeaderDictionary(Action<IDictionary<string, string>> setup)
        {
            var h = CreateHeaderDictionary();
            setup(h);
            return h;
        }

        public static AppDelegate Simple()
        {
            return new FakeApp().AppDelegate;
        }

        public static AppDelegate Simple(string status)
        {
            return new FakeApp(status).AppDelegate;
        }

        public static AppDelegate Simple(string status, IDictionary<string, string> headers)
        {
            return new FakeApp(status) { Headers = headers }.AppDelegate;
        }

        public static AppDelegate Simple(string status, IDictionary<string, string> headers, string body)
        {
            return new FakeApp(status, body) {Headers = headers}.AppDelegate;
        }

        public static AppDelegate Simple(string status, IDictionary<string, string> headers, BodyDelegate body)
        {
            return new FakeApp(status, body) {Headers = headers}.AppDelegate;
        }

        public static AppDelegate Simple(string status, BodyDelegate body)
        {
            return new FakeApp(status, body).AppDelegate;
        }

        public static IAppBuilder Simple(this IAppBuilder builder)
        {
            return builder.Run(Simple());
        }

        public static IAppBuilder Simple(this IAppBuilder builder, string status)
        {
            return builder.Run(Simple(status));
        }

        public static IAppBuilder Simple(this IAppBuilder builder, string status, BodyDelegate body)
        {
            return builder.Run(Simple(status, body));
        }

        public static IAppBuilder Simple(this IAppBuilder builder, string status, IDictionary<string, string> headers)
        {
            return builder.Run(Simple(status, headers));
        }

        public static IAppBuilder Simple(this IAppBuilder builder, string status, IDictionary<string, string> headers, BodyDelegate body)
        {
            return builder.Run(Simple(status, headers, body));
        }

        public static IAppBuilder Simple(this IAppBuilder builder, string status, IDictionary<string, string> headers, string body)
        {
            return builder.Run(Simple(status, headers, body));
        }
    }
}