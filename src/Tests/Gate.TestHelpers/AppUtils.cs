using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Gate.Helpers;

namespace Gate.TestHelpers
{
    public class AppUtils
    {
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

        public static AppDelegate Simple(string status, IDictionary<string, string> headers, string body)
        {
            return new FakeApp(status, body) {Headers = headers}.AppDelegate;
        }
    }
}