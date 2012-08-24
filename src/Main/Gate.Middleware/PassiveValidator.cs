using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gate.Middleware;
using System.Threading.Tasks;
using Gate;
using Owin;

namespace Owin
{
    public static class ValidatorExtensions
    {
        public static IAppBuilder UsePassiveValidator(this IAppBuilder builder)
        {
            return builder.UseType<PassiveValidator>();
        }
    }
}

namespace Gate.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // This middleware does a passive validation of all requests and responses. If any requirements from the OWIN 
    // standard are violated, an 500 error or a warning header are returned to the client.
    // This is implemented using version 0.14.0 of the OWIN standard.
    public class PassiveValidator
    {
        private readonly AppFunc nextApp;

        public PassiveValidator(AppFunc nextApp)
        {
            this.nextApp = nextApp;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            IList<string> warnings = new List<string>();
            if (!TryValidateCall(env, warnings))
            {
                return TaskHelpers.Completed();
            }

            try
            {
                return nextApp(env)
                    .Then(() =>
                    {
                        if (!TryValidateResult(env, warnings))
                        {
                            return;
                        }

                        if (warnings.Count > 0)
                        {
                            var headers = env.Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders);
                            headers["X-OwinValidatorWarning"] = warnings.ToArray();
                        }
                    })
                    .Catch(errorInfo =>
                    {
                        SetFatalResult(env, "6.1", "An asynchronous exception was thrown from the AppDelegate: \r\n"
                            + errorInfo.Exception.ToString());
                        return errorInfo.Handled();
                    });
            }
            catch (Exception ex)
            {
                SetFatalResult(env, "6.1", "A synchronous exception was thrown from the AppDelegate: \r\n"
                            + ex.ToString());
                return TaskHelpers.Completed();
            }
        }

        #region Call Rules

        // Returns false for fatal errors, along with a resulting message.
        // Otherwise any warnings are appended.
        private static bool TryValidateCall(IDictionary<string, object> env, IList<string> warnings)
        {
            if (env == null)
            {
                throw new ArgumentNullException("env");
            }
            
            // Must be mutable
            try
            {
                string key = "validator.MutableKey";
                string input = "Mutable Value";
                env[key] = input;
                string output = env.Get<string>(key);
                if (output == null || output != input)
                {
                    SetFatalResult(env, "3.3", "CallParameters.Environment is not fully mutable.");
                    return false;
                }
                env.Remove(key);
            }
            catch (Exception ex)
            {
                SetFatalResult(env, "3.3", "CallParameters.Environment is not mutable: \r\n" + ex.ToString());
                return false;
            }

            // Environment key names MUST be case sensitive.
            string upperKey = "Validator.CaseKey";
            string lowerKey = "validator.casekey";
            string[] caseValue = new string[] { "Case Value" };
            env[upperKey] = caseValue;
            string[] resultValue = env.Get<string[]>(lowerKey);
            if (resultValue != null)
            {
                SetFatalResult(env, "3.3", "CallParameters.Environment is not case sensitive.");
                return false;
            }
            env.Remove(upperKey);
            
            // Check for required owin.* keys and the HOST header.
            if (!CheckRequiredCallData(env, warnings))
            {
                return false;
            }

            return true;
        }

        private static bool CheckRequiredCallData(IDictionary<string, object> env, IList<string> warnings)
        {
            string[] requiredKeys = new string[]
            {
                "owin.CallCompleted",
                "owin.RequestBody",
                "owin.RequestHeaders",
                "owin.RequestMethod",
                "owin.RequestPath",
                "owin.RequestPathBase",
                "owin.RequestProtocol",
                "owin.RequestQueryString",
                "owin.RequestScheme",

                "owin.ResponseHeaders",
                "owin.ResponseBody",

                "owin.Version"
            };

            object temp;
            foreach (string key in requiredKeys)
            {
                if (!env.TryGetValue(key, out temp))
                {
                    SetFatalResult(env, "3.3", "Missing required Environment key: " + key);
                    return false;
                }

                if (temp == null)
                {
                    SetFatalResult(env, "3.3", "Required Environment value is null: " + key);
                    return false;
                }
            }

            IDictionary<string, string[]> requestHeaders = env.Get<IDictionary<string, string[]>>("owin.RequestHeaders");
            IDictionary<string, string[]> responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");

            if (!TryValidateHeaderCollection(env, requestHeaders, "Request", warnings))
            {
                return false;
            }

            if (!TryValidateHeaderCollection(env, responseHeaders, "Response", warnings))
            {
                return false;
            }

            string[] header;
            if (!requestHeaders.TryGetValue("HOST", out header) || header.Length == 0)
            {
                SetFatalResult(env, "5.2", "Missing Host header");
                return false;
            }
            
            // Validate values

            string[] stringValueTypes = new string[]
            {
                "owin.RequestMethod",
                "owin.RequestPath",
                "owin.RequestPathBase",
                "owin.RequestProtocol",
                "owin.RequestQueryString",
                "owin.RequestScheme",
                "owin.Version" 
            };

            foreach (string key in stringValueTypes)
            {
                if (!(env[key] is string))
                {
                    SetFatalResult(env, "3.3", key + " value is not of type string: " + env[key].GetType().FullName);
                    return false;
                }
            }

            if (!(env["owin.CallCompleted"] is Task))
            {
                SetFatalResult(env, "3.3", "owin.CallCompleted is not of type Task: " + env["owin.CallCompleted"].GetType().FullName);
                return false;
            }

            if (env.Get<Task>("owin.CallCompleted").IsCompleted)
            {
                warnings.Add(CreateWarning("3.9", "The owin.CallCompleted Task was complete before processing the request."));
            }

            if (string.IsNullOrWhiteSpace(env.Get<string>("owin.RequestMethod")))
            {
                SetFatalResult(env, "3.3", "owin.RequestMethod is empty.");
                return false;
            }

            if (!env.Get<string>("owin.RequestPath").StartsWith("/"))
            {
                SetFatalResult(env, "5.3", "owin.RequestPath does not start with a slash.");
                return false;
            }

            if (env.Get<string>("owin.RequestPathBase").EndsWith("/"))
            {
                SetFatalResult(env, "5.3", "owin.RequestBasePath ends with a slash.");
                return false;
            }
            
            string protocol = env.Get<string>("owin.RequestProtocol");
            if (!protocol.Equals("HTTP/1.1", StringComparison.OrdinalIgnoreCase)
                && !protocol.Equals("HTTP/1.0", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(CreateWarning("3.3", "Unrecognized request protocol: " + protocol));
            }

            // No query string validation.

            string scheme = env.Get<string>("owin.RequestScheme");
            if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(CreateWarning("5.1", "Unrecognized request scheme: " + scheme));
            }

            string version = env.Get<string>("owin.Version");
            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
            {
                SetFatalResult(env, "7", "owin.Version could not be parsed: " + version);
                return false;
            }

            if (!parsedVersion.Equals(new Version(1, 0)))
            {
                warnings.Add(CreateWarning("7", "Unrecognized OWIN version: " + version));
            }
            
            return true;
        }

        // Shared code for validating that the request and response header collections adhere to some basic requirements like mutability and casing.
        private static bool TryValidateHeaderCollection(IDictionary<string, object> env, IDictionary<string, string[]> headers, 
            string headerId, IList<string> warnings)
        {
            // Must be mutable
            try
            {
                string key = "x-validator-mutate";
                string[] input = new string[] { "Mutable Value" };
                headers[key] = input;
                string[] output = headers[key];
                if (output == null || output.Length != input.Length || output[0] != input[0])
                {
                    SetFatalResult(env, "3.7", headerId + " headers are not fully mutable.");
                    return false;
                }
                headers.Remove(key);
            }
            catch (Exception ex)
            {
                SetFatalResult(env, "3.7", headerId + " headers are not mutable: \r\n" + ex.ToString());
                return false;
            }

            // Header key names MUST be case insensitive.
            string upperKey = "X-Validator-Case";
            string lowerKey = "x-validator-case";
            string[] caseValue = new string[] { "Case Value" };
            headers[upperKey] = caseValue;
            string[] resultValue = null;
            if (!headers.TryGetValue(lowerKey, out resultValue) || resultValue.Length != caseValue.Length || resultValue[0] != caseValue[0])
            {
                SetFatalResult(env, "3.7", headerId + " headers are case sensitive.");
                return false;
            }
            headers.Remove(upperKey);

            foreach (var pair in headers)
            {
                if (pair.Value == null)
                {
                    warnings.Add(CreateWarning("3.7", headerId + " header " + pair.Key + " has a null string[]."));
                }
                else
                {
                    for (int i = 0; i < pair.Value.Length; i++)
                    {
                        if (pair.Value[i] == null)
                        {
                            warnings.Add(CreateWarning("3.7", headerId + " header " + pair.Key + " has a null value at index " + i));
                        }
                    }
                }
            }

            return true;
        }

        #endregion Call Rules

        #region Result Rules

        private static bool TryValidateResult(IDictionary<string, object> env, IList<string> warnings)
        {
            IDictionary<string, string[]> responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");

            if (!TryValidateHeaderCollection(env, responseHeaders, "Response", warnings))
            {
                return false;
            }

            return true;
        }

        #endregion Result Rules

        private static void SetFatalResult(IDictionary<string, object> env, string standardSection, string message)
        {
            Response response = new Response(env);
            response.StatusCode = 500;
            response.ReasonPhrase = "Internal Server Error";
            response.Write("OWIN v0.14.0 validation failure: Section#{0}, {1}", standardSection, message);
            response.End();
        }

        private static string CreateWarning(string standardSection, string message)
        {
            return string.Format("OWIN v0.14.0 validation warning: Section#{0}, {1}", standardSection, message);
        }
    }
}
