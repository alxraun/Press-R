using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PressR.Debugger
{
    public class DebuggerConfigManager
    {
        private const string LogPrefix = "[Debugger] ";

        public IEnumerable<IDebuggerConfig> AvailableConfigs { get; private set; }
        public IDebuggerConfig CurrentConfig { get; private set; }
        public string ConfigLoadingError { get; private set; }
        public List<DebuggerTrackedValueInfo> CurrentTrackedValues { get; private set; } =
            new List<DebuggerTrackedValueInfo>();

        public void Initialize()
        {
            var configTypes = typeof(IDebuggerConfig)
                .Assembly.GetTypes()
                .Where(t =>
                    typeof(IDebuggerConfig).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract
                    && t.GetConstructor(Type.EmptyTypes) != null
                );

            var configs = new List<IDebuggerConfig>();
            foreach (var type in configTypes)
            {
                try
                {
                    var configInstance = (IDebuggerConfig)Activator.CreateInstance(type);
                    configs.Add(configInstance);
                }
                catch (Exception ex)
                {
                    DebuggerLog.Warning(
                        $"{LogPrefix}Failed to instantiate IDebuggerConfig type '{type.FullName}': {ex.Message}"
                    );
                }
            }

            AvailableConfigs = configs.OrderBy(c => c.Name).ToList();
            DebuggerLog.Info($"{LogPrefix}Found {AvailableConfigs.Count()} configurations.");

            if (AvailableConfigs.Any())
            {
                LoadConfig(AvailableConfigs.First());
            }
            else
            {
                LoadConfig(null);
            }
        }

        public void LoadConfig(IDebuggerConfig config)
        {
            CurrentConfig = config;
            CurrentTrackedValues.Clear();
            ConfigLoadingError = null;

            if (config != null)
            {
                try
                {
                    CurrentTrackedValues =
                        config.GetTrackedValues()?.ToList() ?? new List<DebuggerTrackedValueInfo>();
                }
                catch (Exception ex)
                {
                    DebuggerLog.Warning(
                        $"{LogPrefix}Error getting tracked values from config '{config.Name}': {ex.Message}"
                    );
                    CurrentTrackedValues.Clear();
                    ConfigLoadingError = $"Failed to load config '{config.Name}': {ex.Message}";
                }

                if (!CurrentTrackedValues.Any() && ConfigLoadingError == null)
                {
                    ConfigLoadingError = "No tracked values found in this configuration.";
                }
            }
            else
            {
                ConfigLoadingError = "No configurations available.";
            }

            DebuggerLog.Info(
                $"{LogPrefix}Config loaded: {config?.Name ?? "None"}. Tracking {CurrentTrackedValues.Count} values."
            );
        }
    }
}
