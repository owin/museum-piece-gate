using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Bootstrapper;

namespace Gate.Nancy
{
    using ResultDelegate = Action< // result
        string, // status
        IDictionary<string, string>, // headers
        Func< // body
            Func< // next
                ArraySegment<byte>, // data
                Action, // continuation
                bool>, // async                    
            Action<Exception>, // error
            Action, // complete
            Action>>; // cancel

    public class Application
    {
        readonly INancyEngine _engine;

        public Application(INancyEngine engine)
        {
            _engine = engine;
        }

        public static Action<IDictionary<string, object>, ResultDelegate, Action<Exception>> Create()
        {
            var bootstrapper = NancyBootstrapperLocator.Bootstrapper;
            bootstrapper.Initialise();

            var application = new Application(bootstrapper.GetEngine());
            return application.Call;
        }

        public void Call(
            IDictionary<string, object> env,
            ResultDelegate result,
            Action<Exception> fault)
        {
            var nancyRequest = CreateNancyRequestFromEnvironment(env);
            var nancyContext = _engine.HandleRequest(nancyRequest);
            SendNancyResponseToResult(nancyContext.Response, result);
        }

        static Request CreateNancyRequestFromEnvironment(IDictionary<string, object> env)
        {
            var environment = new Environment(env);
            var request = new Request(
                environment.Method,
                environment.Path,
                environment.Headers.ToDictionary(
                    kv => kv.Key,
                    kv => (IEnumerable<string>) kv.Value.Split(new[] {'\r', 'n'}, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase),
                new InputStream(environment.Body),
                environment.Scheme,
                environment.QueryString);

            return request;
        }


        static void SendNancyResponseToResult(Response response, ResultDelegate result)
        {
            if (!response.Headers.ContainsKey("Content-Type") && !string.IsNullOrWhiteSpace(response.ContentType))
                response.Headers["Content-Type"] = response.ContentType;

            result(
                string.Format("{0:000} UNK", (int) response.StatusCode),
                response.Headers,
                (next, error, complete) =>
                {
                    using (var stream = new OutputStream(next, complete))
                    {
                        try
                        {
                            response.Contents(stream);
                        }
                        catch (Exception ex)
                        {
                            error(ex);
                        }
                    }
                    return () => { };
                });
        }
    }
}