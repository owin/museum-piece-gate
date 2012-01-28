using System;

namespace Ghost.Engine
{
    public interface IGhostEngine
    {
        IDisposable Start(StartInfo info);
    }
}