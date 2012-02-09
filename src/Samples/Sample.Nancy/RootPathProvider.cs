using System;
using Nancy;

namespace Sample.Nancy
{
    public class RootPathProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        }
    }
}