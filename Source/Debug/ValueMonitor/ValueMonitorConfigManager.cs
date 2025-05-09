using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PressR.Debug.ValueMonitor
{
    public class ValueMonitorConfigManager
    {
        private const string LogPrefix = "[ValueMonitor] ";

        public IEnumerable<IValueMonitorConfig> AvailableConfigs { get; private set; }
        public IValueMonitorConfig CurrentConfig { get; private set; }
        public string ConfigLoadingError { get; private set; }
        public List<ValueMonitorTrackedValueInfo> CurrentTrackedValues { get; private set; } =
            new List<ValueMonitorTrackedValueInfo>();

        public void Initialize()
        {
            var configTypes = typeof(IValueMonitorConfig)
                .Assembly.GetTypes()
                .Where(t =>
                    typeof(IValueMonitorConfig).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract
                    && t.GetConstructor(Type.EmptyTypes) != null
                );

            var configs = new List<IValueMonitorConfig>();
            foreach (var type in configTypes)
            {
                try
                {
                    var configInstance = (IValueMonitorConfig)Activator.CreateInstance(type);
                    configs.Add(configInstance);
                }
                catch (Exception ex)
                {
                    ValueMonitorLog.Warning(
                        $"{LogPrefix}Failed to instantiate IValueMonitorConfig type '{type.FullName}': {ex.Message}"
                    );
                }
            }

            AvailableConfigs = configs.OrderBy(c => c.Name).ToList();
            ValueMonitorLog.Info($"{LogPrefix}Found {AvailableConfigs.Count()} configurations.");

            if (AvailableConfigs.Any())
            {
                LoadConfig(AvailableConfigs.First());
            }
            else
            {
                LoadConfig(null);
            }
        }

        public void LoadConfig(IValueMonitorConfig config)
        {
            CurrentConfig = config;
            CurrentTrackedValues.Clear();
            ConfigLoadingError = null;

            if (config != null)
            {
                try
                {
                    CurrentTrackedValues =
                        config.GetTrackedValues()?.ToList()
                        ?? new List<ValueMonitorTrackedValueInfo>();
                }
                catch (Exception ex)
                {
                    ValueMonitorLog.Warning(
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

            ValueMonitorLog.Info(
                $"{LogPrefix}Config loaded: {config?.Name ?? "None"}. Tracking {CurrentTrackedValues.Count} values."
            );
        }
    }
}
