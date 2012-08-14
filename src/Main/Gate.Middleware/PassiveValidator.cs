using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            if (!CheckCallDictionaries(call, out fatalResult))
            {
                return false;
            }            

            return true;
        }

        private static bool CheckCallDictionaries(CallParameters call, out ResultParameters fatalResult)
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
            if (resultValue != caseValue || resultValue[0] != caseValue[0])
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
            if (!call.Headers.TryGetValue(lowerKey, out resultValue) || resultValue != caseValue || resultValue[0] != caseValue[0])
            {
                fatalResult = CreateFatalResult("3.7", "CallParameters.Headers is case sensitive.");
                return false;
            }
            call.Headers.Remove(upperKey);

            // TODO: Check for required owin.* keys and the HOST header.

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
                fatalResult = CreateFatalResult("3.7", "ResultParameters.Properties is not mutable: \r\n" + ex.ToString());
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
            if (resultValue != caseValue || resultValue[0] != caseValue[0])
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
            if (!result.Headers.TryGetValue(lowerKey, out resultValue) || resultValue != caseValue || resultValue[0] != caseValue[0])
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
            response.Write("OWIN v0.14.0 validation failure: Section#{1}, {2}", standardSection, message);
            response.End();
            return response.Result;
        }
    }
}
