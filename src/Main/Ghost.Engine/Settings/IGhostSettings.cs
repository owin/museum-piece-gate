using System.IO;
using Gate.Builder.Loader;
using Gate.Owin;

namespace Ghost.Engine.Settings
{
    public interface IGhostSettings
    {
        string DefaultServer { get; }
        string DefaultScheme { get; }
        string DefaultHost { get; }
        int? DefaultPort { get; }
        TextWriter DefaultOutput { get; }

        string ServerAssemblyPrefix { get; }

        IStartupLoader Loader { get; }
        IAppBuilder Builder { get; }
    }
}
