using System;
using Nancy;

namespace Gate.Nancy {
    public class ApplicationBaseRootPathProvider : IRootPathProvider{
        public string GetRootPath()
        {
            return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        }
    }
}
