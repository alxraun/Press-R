using System.Collections.Generic;

namespace PressR.Debug.ValueMonitor
{
    public interface IValueMonitorConfig
    {
        string Name { get; }

        float UpdateInterval { get; }

        float StartDelaySeconds { get; }

        IEnumerable<ValueMonitorTrackedValueInfo> GetTrackedValues();
    }
}
