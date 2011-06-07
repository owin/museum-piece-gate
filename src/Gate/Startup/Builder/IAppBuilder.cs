using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Startup
{
    public interface IAppBuilder
    {
        IAppBuilder Use(Func<AppDelegate, AppDelegate> factory);
        AppDelegate Build();
    }
}
