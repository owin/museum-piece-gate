using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gate.Middleware;
using System.Threading.Tasks;
using Gate;

namespace Owin
{
    // This middleware does a passive validation of all requests and responses. If any requirements from the OWIN 
    // standard are violated, an 500 error or a warning header are returned to the client.
    // This is implemented using version 0.14.0 of the OWIN standard.
    public static class PassiveValidator
    {
        public static IAppBuilder UsePassiveValidation(this IAppBuilder builder)
        {
            // TODO: Validate the builder.Properties collection.
            return builder.UseFunc<AppDelegate>(PassiveValidator.Middleware);
        }

        public static AppDelegate Middleware(AppDelegate app)
        {
            return call =>
            {
                IList<string> warnings = new List<string>();
                ResultParameters fatalResult;
                if (!TryValidateCall(call, warnings, out fatalResult))
                {
                    return TaskHelpers.FromResult(fatalResult);
                }

                try
                {
                    return app(call)
                        .Then(appResult =>
                        {
                            if (!TryValidateResult(call, appResult, warnings, out fatalResult))
                            {
                                return fatalResult;
                            }

                            if (warnings.Count > 0)
                            {
                                appResult.Headers["X-OwinValidatorWarning"] = warnings.ToArray();
                            }

                            // Intercept the body delegate for parameter validation and error handling validation.
                            Func<Stream, Task> nestedBody = appResult.Body;
                            appResult.Body = output =>
                            {
                                // TODO: Validate output stream.
                                // TODO: How to report errors for things like a null output stream?  Exceptions?

                                if (nestedBody == null)
                                {
                                    return TaskHelpers.Completed();
                                }
                                
                                try
                                {
                                    return nestedBody(output)
                                        .Then(() => { /* TODO: Any post body validation required? */ })
                                        .Catch(errorInfo =>
                                        {
                                            // TODO: Write out exception?
                                            // return errorInfo.Handled();
                                            return errorInfo.Throw();
                                        });
                                }
                                catch (Exception)
                                {
                                    // TODO: Write out exception?
                                    // return TaskHelpers.Completed();
                                    throw;
                                }
                            };

                            return appResult;
                        })
                        .Catch<ResultParameters>(errorInfo =>
                        {
                            ResultParameters errorResult = CreateFatalResult("6.1", "An asynchronous exception was thrown from the AppDelegate: \r\n"
                                + errorInfo.Exception.ToString());
                            return errorInfo.Handled(errorResult);
                        });
                }
                catch (Exception ex)
                {
                    return TaskHelpers.FromResult(CreateFatalResult("6.1", "A synchronous exception was thrown from the AppDelegate: \r\n"
                                + ex.ToString()));
                }
            };
        }

        #region Call Rules

        // Returns false for fatal errors, along with a resulting message.
        // Otherwise any warnings are appended.
        private static bool TryValidateCall(CallParameters call, IList<string> warnings, out ResultParameters fatalResult)
        {
            if (!CheckCallDictionaries(call, warnings, out fatalResult))
            {
                return false;
            }            

            return true;
        }

        private static bool CheckCallDictionaries(CallParameters call, IList<string> warnings, out ResultParameters fatalResult)
        {
            fatalResult = default(ResultParameters);
            if (call.Environment == null)
            {
                fatalResult = CreateFatalResult("3.3", "CallParameters.Environment is null.");
                return false;
            }
            
            if (call.Headers == null)
            {
                fatalResult = CreateFatalResult("3.7", "CallParameters.Headers is null.");
                return false;
            }
            
            // Must be mutable
            try
            {
                string key = "validator.MutableKey";
                string input = "Mutable Value";
                call.Environment[key] = input;
                string output = call.Environment.Get<string>(key);
                if (output == null || output != input)
                {
                    fatalResult = CreateFatalResult("3.3", "CallParameters.Environment is not fully mutable.");
                    return false;
                }
                call.Environment.Remove(key);
            }
            catch (Exception ex)
            {
                fatalResult = CreateFatalResult("3.3", "CallParameters.Environment is not mutable: \r\n" + ex.ToString());
            }

            // Must be mutable
            try
            {
                string key = "x-validator-mutate";
                string[] input = new string[] { "Mutable Value" };
                call.Headers[key] = input;
                string[] output = call.Headers[key];
                if (output == null || output.Length != input.Length || output[0] != input[0])
                {
                    fatalResult = CreateFatalResult("3.7", "CallParameters.Headers is not fully mutable.");
                    return false;
                }
                call.Headers.Remove(key);
            }
            catch (Exception ex)
            {
                fatalResult = CreateFatalResult("3.7", "CallParameters.Headers is not mutable: \r\n" + ex.ToString());
            }

            // Environment key names MUST be case sensitive.
            string upperKey = "Validator.CaseKey";
            string lowerKey = "validator.casekey";
            string[] caseValue = new string[] { "Case Value" };
            call.Environment[upperKey] = caseValue;
            string[] resultValue = call.Environment.Get<string[]>(lowerKey);
            if (resultValue != null)
            {
                fatalResult = CreateFatalResult("3.3", "CallParameters.Environment is not case sensitive.");
                return false;
            }
            call.Environment.Remove(upperKey);

            // Header key names MUST be case insensitive.
            upperKey = "X-Validator-Case";
            lowerKey = "x-validator-case";
            caseValue = new string[] { "Case Value" };
            call.Headers[upperKey] = caseValue;
            resultValue = null;
            if (!call.Headers.TryGetValue(lowerKey, out resultValue) || resultValue.Length != caseValue.Length || resultValue[0] != caseValue[0])
            {
                fatalResult = CreateFatalResult("3.7", "CallParameters.Headers is case sensitive.");
                return false;
            }
            call.Headers.Remove(upperKey);

            // Check for required owin.* keys and the HOST header.
            if (!CheckRequiredCallData(call, warnings, out fatalResult))
            {
                return false;
            }

            return true;
        }

        private static bool CheckRequiredCallData(CallParameters call, IList<string> warnings, out ResultParameters fatalResult)
        {
            fatalResult = default(ResultParameters);
            string[] requiredKeys = new string[]
            {
                "owin.CallCompleted",
                "owin.RequestMethod",
                "owin.RequestPath",
                "owin.RequestPathBase",
                "owin.RequestProtocol",
                "owin.RequestQueryString",
                "owin.RequestScheme",
                "owin.Version" 
            };

            object temp;
            foreach (string key in requiredKeys)
            {
                if (!call.Environment.TryGetValue(key, out temp))
                {
                    fatalResult = CreateFatalResult("3.3", "Missing required Environment key: " + key);
                    return false;
                }

                if (temp == null)
                {
                    fatalResult = CreateFatalResult("3.3", "Required Environment value is null: " + key);
                    return false;
                }
            }

            string[] header;
            if (!call.Headers.TryGetValue("HOST", out header) || header.Length == 0)
            {
                fatalResult = CreateFatalResult("5.2", "Missing Host header");
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
                if (!(call.Environment[key] is string))
                {
                    fatalResult = CreateFatalResult("3.3", key + " value is not of type string: " + call.Environment[key].GetType().FullName);
                    return false;
                }
            }

            if (!(call.Environment["owin.CallCompleted"] is Task))
            {
                fatalResult = CreateFatalResult("3.3", "owin.CallCompleted is not of type Task: " + call.Environment["owin.CallCompleted"].GetType().FullName);
                return false;
            }

            if (call.Environment.Get<Task>("owin.CallCompleted").IsCompleted)
            {
                warnings.Add(CreateWarning("3.9", "The owin.CallCompleted Task was complete before processing the request."));
            }

            if (string.IsNullOrWhiteSpace(call.Environment.Get<string>("owin.RequestMethod")))
            {
                fatalResult = CreateFatalResult("3.3", "owin.RequestMethod is empty.");
                return false;
            }

            if (!call.Environment.Get<string>("owin.RequestPath").StartsWith("/"))
            {
                fatalResult = CreateFatalResult("5.3", "owin.RequestPath does not start with a slash.");
                return false;
            }

            if (call.Environment.Get<string>("owin.RequestPathBase").EndsWith("/"))
            {
                fatalResult = CreateFatalResult("5.3", "owin.RequestBasePath ends with a slash.");
                return false;
            }
            
            string protocol = call.Environment.Get<string>("owin.RequestProtocol");
            if (!protocol.Equals("HTTP/1.1", StringComparison.OrdinalIgnoreCase)
                && !protocol.Equals("HTTP/1.0", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(CreateWarning("3.3", "Unrecognized request protocol: " + protocol));
            }

            // No query string validation.

            string scheme = call.Environment.Get<string>("owin.RequestScheme");
            if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(CreateWarning("5.1", "Unrecognized request scheme: " + scheme));
            }

            string version = call.Environment.Get<string>("owin.Version");
            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
            {
                fatalResult = CreateFatalResult("7", "owin.Version could not be parsed: " + version);
                return false;
            }

            if (!parsedVersion.Equals(new Version(1, 0)))
            {
                warnings.Add(CreateWarning("7", "Unrecognized OWIN version: " + version));
            }
            
            return true;
        }

        #endregion Call Rules

        #region Result Rules

        private static bool TryValidateResult(CallParameters call, ResultParameters appResult, IList<string> warnings, out ResultParameters fatalResult)
        {
            if (!CheckResultDictionaries(appResult, out fatalResult))
            {
                return false;
            }

            return true;
        }

        private static bool CheckResultDictionaries(ResultParameters result, out ResultParameters fatalResult)
        {
            fatalResult = default(ResultParameters);
            if (result.Properties == null)
            {
                fatalResult = CreateFatalResult("3.6", "ResultParameters.Properties is null.");
                return false;
            }

            if (result.Headers == null)
            {
                fatalResult = CreateFatalResult("3.7", "ResultParameters.Headers is null.");
                return false;
            }
            
            // Must be mutable
            try
            {
                string key = "validator.MutableKey";
                string input = "Mutable Value";
                result.Properties[key] = input;
                string output = result.Properties.Get<string>(key);
                if (output == null || output != input)
                {
                    fatalResult = CreateFatalResult("3.6", "ResultParameters.Properties is not fully mutable.");
                    return false;
                }
                result.Properties.Remove(key);
            }
            catch (Exception ex)
            {
                fatalResult = CreateFatalResult("3.6", "ResultParameters.Properties is not mutable: \r\n" + ex.ToString());
            }

            // Must be mutable
            try
            {
                string key = "x-validator-mutate";
                string[] input = new string[] { "Mutable Value" };
                result.Headers[key] = input;
                string[] output = result.Headers[key];
                if (output == null || output.Length != input.Length || output[0] != input[0])
                {
                    fatalResult = CreateFatalResult("3.7", "ResultParameters.Headers is not fully mutable.");
                    return false;
                }
                result.Headers.Remove(key);
            }
            catch (Exception ex)
            {
                fatalResult = CreateFatalResult("3.7", "ResultParameters.Headers is not mutable: \r\n" + ex.ToString());
            }

            // Properties key names MUST be case sensitive.
            string upperKey = "Validator.CaseKey";
            string lowerKey = "validator.casekey";
            string[] caseValue = new string[] { "Case Value" };
            result.Properties[upperKey] = caseValue;
            string[] resultValue = result.Properties.Get<string[]>(lowerKey);
            if (resultValue != null)
            {
                fatalResult = CreateFatalResult("3.6", "ResultParameters.Properties is not case sensitive.");
                return false;
            }
            result.Properties.Remove(upperKey);

            // Header key names MUST be case insensitive.
            upperKey = "X-Validator-Case";
            lowerKey = "x-validator-case";
            caseValue = new string[] { "Case Value" };
            result.Headers[upperKey] = caseValue;
            resultValue = null;
            if (!result.Headers.TryGetValue(lowerKey, out resultValue) || resultValue.Length != caseValue.Length || resultValue[0] != caseValue[0])
            {
                fatalResult = CreateFatalResult("3.7", "ResultParameters.Headers is case sensitive.");
                return false;
            }
            result.Headers.Remove(upperKey);

            return true;
        }

        #endregion Result Rules

        private static ResultParameters CreateFatalResult(string standardSection, string message)
        {
            Response response = new Response(500);
            response.Write("OWIN v0.14.0 validation failure: Section#{0}, {1}", standardSection, message);
            response.End();
            return response.Result;
        }

        private static string CreateWarning(string standardSection, string message)
        {
            return string.Format("OWIN v0.14.0 validation warning: Section#{0}, {1}", standardSection, message);
        }
    }
}
