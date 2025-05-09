using System.Collections.Generic;

namespace PressR.Debugger
{
    public interface IDebuggerConfig
    {
        string Name { get; }

        float UpdateInterval { get; }

        float StartDelaySeconds { get; }

        IEnumerable<DebuggerTrackedValueInfo> GetTrackedValues();
    }
}
