using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Debugger.Resolver;
using UnityEngine;
using Verse;

namespace PressR.Debugger
{
    public static class DebuggerCore
    {
        private static DebuggerConfigManager _configManager;
        private static DebuggerStateManager _stateManager;
        private static DebuggerSnapshotManager _snapshotManager;
        private static ValueResolver _valueResolver;
        private const string LogPrefix = "[Debugger] ";

        private static float _lastUiRefreshRealTime = -1f;
        private static float _uiRefreshInterval = 1.0f;

        public static IEnumerable<IDebuggerConfig> AvailableConfigs =>
            _configManager?.AvailableConfigs;
        public static IDebuggerConfig CurrentConfig => _configManager?.CurrentConfig;
        public static string ConfigLoadingError => _configManager?.ConfigLoadingError;
        public static RecordingState CurrentRecordingState =>
            _stateManager?.CurrentRecordingState ?? RecordingState.Stopped;
        public static int MaxHistorySize
        {
            get => _snapshotManager?.MaxHistorySize ?? 0;
            set
            {
                if (_snapshotManager != null)
                    _snapshotManager.MaxHistorySize = value;
            }
        }
        public static List<Dictionary<string, object>> SnapshotsHistory =>
            _snapshotManager?.SnapshotsHistory;
        public static Dictionary<string, object> LastSnapshot => _snapshotManager?.LastSnapshot;

        public static void Initialize()
        {
            _valueResolver = new ValueResolver(new MemoryResolverCache(), new ExpressionCompiler());
            _configManager = new DebuggerConfigManager();
            _stateManager = new DebuggerStateManager();
            _snapshotManager = new DebuggerSnapshotManager(_valueResolver);

            _configManager.Initialize();

            SyncConfigAcrossManagers(_configManager.CurrentConfig);
            _stateManager.SetOnRecordingStartedAction(HandleRecordingStarted);

            UpdateUiRefreshInterval();
        }

        public static void LoadConfig(IDebuggerConfig config)
        {
            if (_configManager == null || _stateManager == null || _snapshotManager == null)
                return;

            _stateManager.StopRecording();
            _configManager.LoadConfig(config);
            _snapshotManager.ClearHistory();
            SyncConfigAcrossManagers(config);
            UpdateUiRefreshInterval();
            _snapshotManager.TakeSnapshot();
        }

        private static void SyncConfigAcrossManagers(IDebuggerConfig config)
        {
            if (_configManager == null || _stateManager == null || _snapshotManager == null)
                return;

            _stateManager.SetConfig(config);
            _snapshotManager.SetTrackedValues(_configManager.CurrentTrackedValues);
        }

        private static void UpdateUiRefreshInterval()
        {
            _uiRefreshInterval = CurrentConfig?.UpdateInterval ?? 1.0f;

            if (_uiRefreshInterval <= 0.01f)
                _uiRefreshInterval = 0.016f;
            _lastUiRefreshRealTime = -1f;
        }

        public static void StartRecording()
        {
            if (_configManager == null || _stateManager == null || _snapshotManager == null)
                return;

            if (_configManager.CurrentConfig == null || !_configManager.CurrentTrackedValues.Any())
            {
                DebuggerLog.Warning(
                    $"{LogPrefix}Cannot start recording: Configuration '{_configManager.CurrentConfig?.Name ?? "None"}' has no valid values to track or is not loaded."
                );
                return;
            }
            _snapshotManager.ClearHistory();

            _snapshotManager.TakeSnapshot();
            _stateManager.StartRecording();
        }

        public static void PauseRecording() => _stateManager?.PauseRecording();

        public static void ResumeRecording() => _stateManager?.ResumeRecording();

        public static void StopRecording()
        {
            _stateManager?.StopRecording();
            _snapshotManager?.TakeSnapshot();
        }

        public static float GetStartDelayTimer() => _stateManager?.GetStartDelayTimer() ?? 0f;

        private static void HandleRecordingStarted(RecordingStartInfo startInfo)
        {
            if (_snapshotManager != null)
            {
                _snapshotManager.SetRecordingStartInfo(startInfo);

                _snapshotManager.TakeSnapshot();
                _snapshotManager.StoreLastSnapshot();
            }
        }

        public static void Tick(float deltaTime)
        {
            if (_stateManager == null || _snapshotManager == null)
                return;

            try
            {
                _stateManager.Update(deltaTime);

                if (_lastUiRefreshRealTime < 0f)
                {
                    _lastUiRefreshRealTime = Time.time;
                }

                if (Time.time >= _lastUiRefreshRealTime + _uiRefreshInterval)
                {
                    _snapshotManager.TakeSnapshot();
                    _lastUiRefreshRealTime = Time.time;
                }

                if (_stateManager.ShouldTakeSnapshot())
                {
                    _snapshotManager.StoreLastSnapshot();
                }
            }
            catch (Exception ex)
            {
                DebuggerLog.Error($"{LogPrefix}Error during Tick: {ex}");
                StopRecording();
            }
        }

        public static string GetHistoryAsCsv()
        {
            if (_snapshotManager == null || _configManager == null || CurrentConfig == null)
            {
                return "Debugger not initialized or no configuration loaded.";
            }
            return DebuggerCsvExporter.GetHistoryAsCsv(
                SnapshotsHistory,
                _configManager.CurrentTrackedValues
            );
        }
    }
}
