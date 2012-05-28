using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Hosting;
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
                DynamicModuleUtility.RegisterModule(typeof(OwinModule));

                var appSetting = ConfigurationManager.AppSettings["Gate.AspNet.SetCurrentDirectory"];
                if (string.IsNullOrWhiteSpace(appSetting) ||
                    string.Equals("Enabled", appSetting, StringComparison.InvariantCultureIgnoreCase))
                {
                    var physicalPath = HostingEnvironment.MapPath("~");
                    if (physicalPath != null)
                    {
                        Directory.SetCurrentDirectory(physicalPath);
                    }
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch
            {
            }
            // ReSharper restore EmptyGeneralCatchClause
        }
    }
}
