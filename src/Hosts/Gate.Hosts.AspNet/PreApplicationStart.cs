using System;
using System.Configuration;
using System.Web;
using Gate.Hosts.AspNet;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Initialize")]

namespace Gate.Hosts.AspNet
{
    public static class PreApplicationStart
    {
        public static void Initialize()
        {
            try
            {
                var appSetting = ConfigurationManager.AppSettings["Gate.Hosts.AspNet.PreApplicationStart"];
                if (string.IsNullOrWhiteSpace(appSetting) ||
                    string.Equals("Enabled", appSetting, StringComparison.InvariantCultureIgnoreCase))
                {

                    DynamicModuleUtility.RegisterModule(typeof(Module));
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            {
                // If we're unable to load MWI then just swallow the exception and don't allow
                // the automagic hub registration
            }
            // ReSharper restore EmptyGeneralCatchClause
        }
    }
}
