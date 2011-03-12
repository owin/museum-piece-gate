using System;
using System.Web;

namespace Gate.AspNet
{
    public class GateModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.AddOnBeginRequestAsync((sender, args, callback, state) => default(IAsyncResult), ar => { });
        }

        public void Dispose()
        {
        }
    }
}
