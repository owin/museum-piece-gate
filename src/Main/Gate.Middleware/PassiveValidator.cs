using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
using System.Threading.Tasks;
using System.IO;

namespace Gate.Middleware
{
    // This middleware does a passive validation of all requests and responses. If any requirements from the OWIN 
    // standard are violated, an 500 error or a warning header are returned to the client.
    public static class PassiveValidator
    {
        public static IAppBuilder UsePassiveValidation(this IAppBuilder builder)
        {
            return builder.Use(PassiveValidator.Middleware);
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
                                appResult.Headers["OwinPassiveValidatorWarning"] = warnings.ToArray();
                            }

                            Func<Stream, Task> nestedBody = appResult.Body;
                            appResult.Body = output =>
                            {
                                // TODO: Validate output stream.
                                // TODO: How to report errors for things like a null output stream?  Exceptions?

                                if (nestedBody != null)
                                {
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
                                }

                                return TaskHelpers.Completed();
                            };

                            // TODO: Intercept the body delegate for parameter validation and error handling validation.
                            return appResult;
                        })
                        .Catch<ResultParameters>(errorInfo =>
                        {
                            ResultParameters errorResult = CreateFatalResult("1", "4.1", 
                                "An asynchronous exception was thrown from the AppDelegate: \r\n" 
                                + errorInfo.Exception.ToString());
                            return errorInfo.Handled(errorResult);
                        });
                }
                catch (Exception ex)
                {
                    return TaskHelpers.FromResult(CreateFatalResult("1", "4.1",
                                "A synchronous exception was thrown from the AppDelegate: \r\n"
                                + ex.ToString()));
                }
            };
        }

        #region Call Rules

        // Returns false for fatal errors, along with a resulting message.
        // Otherwise any warnings are appended.
        private static bool TryValidateCall(CallParameters call, IList<string> warnings, out ResultParameters fatalResult)
        {
            if (!Rule2CheckNullCallDictionariesFatal(call, out fatalResult))
            {
                return false;
            }

            return true;
        }

        private static bool Rule2CheckNullCallDictionariesFatal(CallParameters call, out ResultParameters fatalResult)
        {
            fatalResult = default(ResultParameters);
            if (call.Environment == null)
            {
                fatalResult = CreateFatalResult("2", "2.3", "CallParameters.Environment is null.");
                return false;
            }
            else if (call.Headers == null)
            {
                fatalResult = CreateFatalResult("2", "2.7", "CallParameters.Headers is null.");
                return false;
            }

            return true;
        }

        #endregion Call Rules

        #region Result Rules

        private static bool TryValidateResult(CallParameters call, ResultParameters appResult, IList<string> warnings, out ResultParameters fatalResult)
        {
            if (!Rule2CheckNullResultDictionariesFatal(appResult, out fatalResult))
            {
                return false;
            }

            return true;
        }

        private static bool Rule2CheckNullResultDictionariesFatal(ResultParameters appResult, out ResultParameters fatalResult)
        {
            fatalResult = default(ResultParameters);
            if (appResult.Properties == null)
            {
                fatalResult = CreateFatalResult("2", "2.6", "ResultParameters.Properties is null.");
                return false;
            }
            else if (appResult.Headers == null)
            {
                fatalResult = CreateFatalResult("2", "2.7", "ResultParameters.Headers is null.");
                return false;
            }

            return true;
        }

        #endregion Result Rules

        private static ResultParameters CreateFatalResult(string ruleNumber, string standardSection, string message)
        {
            Response response = new Response(500);
            response.Write("OWIN validation failure: Rule#{0}, Section#{1}, {2}", ruleNumber, standardSection, message);
            response.End();
            return response.Result;
        }
    }
}
