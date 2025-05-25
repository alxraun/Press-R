using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Debug.ValueMonitor.Resolver;
using UnityEngine;
using Verse;

namespace PressR.Debug.ValueMonitor
{
    public static class ValueMonitorCore
    {
        private static ValueMonitorConfigManager _configManager;
        private static ValueMonitorStateManager _stateManager;
        private static ValueMonitorSnapshotManager _snapshotManager;
        private static ValueResolver _valueResolver;
        private const string LogPrefix = "[ValueMonitor] ";

        private static float _lastUiRefreshRealTime = -1f;
        private static float _uiRefreshInterval = 1.0f;

        private static bool _isFullyInitialized = false;

        public static IEnumerable<IValueMonitorConfig> AvailableConfigs =>
            _configManager?.AvailableConfigs;
        public static IValueMonitorConfig CurrentConfig => _configManager?.CurrentConfig;
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
            if (_isFullyInitialized)
                return;

            if (Current.ProgramState != ProgramState.Playing || Find.World == null)
            {
                return;
            }

            _valueResolver = new ValueResolver(new MemoryResolverCache(), new ExpressionCompiler());
            _configManager = new ValueMonitorConfigManager();
            _stateManager = new ValueMonitorStateManager();
            _snapshotManager = new ValueMonitorSnapshotManager(_valueResolver);

            _configManager.Initialize();

            SyncConfigAcrossManagers(_configManager.CurrentConfig);
            _stateManager.SetOnRecordingStartedAction(HandleRecordingStarted);

            UpdateUiRefreshInterval();

            _isFullyInitialized = true;
        }

        public static void LoadConfig(IValueMonitorConfig config)
        {
            if (!_isFullyInitialized)
                return;
            if (_configManager == null || _stateManager == null || _snapshotManager == null)
                return;

            _stateManager.StopRecording();
            _configManager.LoadConfig(config);
            _snapshotManager.ClearHistory();
            SyncConfigAcrossManagers(config);
            UpdateUiRefreshInterval();
            _snapshotManager.TakeSnapshot();
        }

        private static void SyncConfigAcrossManagers(IValueMonitorConfig config)
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
            if (!_isFullyInitialized)
                return;
            if (_configManager == null || _stateManager == null || _snapshotManager == null)
                return;

            if (_configManager.CurrentConfig == null || !_configManager.CurrentTrackedValues.Any())
            {
                ValueMonitorLog.Warning(
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
            if (!_isFullyInitialized)
                return;
            _stateManager?.StopRecording();
            _snapshotManager?.TakeSnapshot();
        }

        public static float GetStartDelayTimer() => _stateManager?.GetStartDelayTimer() ?? 0f;

        private static void HandleRecordingStarted(RecordingStartInfo startInfo)
        {
            if (!_isFullyInitialized)
                return;
            if (_snapshotManager != null)
            {
                _snapshotManager.SetRecordingStartInfo(startInfo);

                _snapshotManager.TakeSnapshot();
                _snapshotManager.StoreLastSnapshot();
            }
        }

        public static void Tick(float deltaTime)
        {
            if (!_isFullyInitialized)
            {
                Initialize();
                if (!_isFullyInitialized)
                    return;
            }

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
                ValueMonitorLog.Error($"{LogPrefix}Error during Tick: {ex}");
                StopRecording();
            }
        }

        public static string GetHistoryAsCsv()
        {
            if (!_isFullyInitialized)
                return "ValueMonitor not initialized.";
            if (_snapshotManager == null || _configManager == null || CurrentConfig == null)
            {
                return "ValueMonitor not fully initialized or no configuration loaded.";
            }
            return ValueMonitorCsvExporter.GetHistoryAsCsv(
                SnapshotsHistory,
                _configManager.CurrentTrackedValues
            );
        }
    }
}
