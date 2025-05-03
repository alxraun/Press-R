using UnityEngine;
using Verse;

namespace PressR.Debugger
{
    public enum RecordingState
    {
        Stopped,
        Starting,
        Recording,
        Paused,
    }

    public class RecordingStartInfo
    {
        public float StartTime { get; }
        public int StartFrame { get; }

        public RecordingStartInfo(float startTime, int startFrame)
        {
            StartTime = startTime;
            StartFrame = startFrame;
        }
    }

    public class DebuggerStateManager
    {
        private const string LogPrefix = "[Debugger] ";

        public RecordingState CurrentRecordingState { get; private set; } = RecordingState.Stopped;
        private float _startDelayTimer;
        private float _lastSnapshotRealTime = -1f;

        private IDebuggerConfig _currentConfig;
        private System.Action<RecordingStartInfo> _onRecordingStarted;
        private RecordingStartInfo _currentRecordingInfo;

        public void SetConfig(IDebuggerConfig config) => _currentConfig = config;

        public void SetOnRecordingStartedAction(System.Action<RecordingStartInfo> action) =>
            _onRecordingStarted = action;

        public void StartRecording()
        {
            if (_currentConfig == null)
            {
                DebuggerLog.Warning($"{LogPrefix}Cannot start recording: No configuration loaded.");
                return;
            }

            StopRecording();

            CurrentRecordingState = RecordingState.Starting;
            _startDelayTimer = _currentConfig.StartDelaySeconds;
            _lastSnapshotRealTime = -1f;

            DebuggerLog.Info($"{LogPrefix}Recording starting in {_startDelayTimer:F1}s...");
            if (_startDelayTimer <= 0f)
            {
                CompleteStart();
            }
        }

        public void PauseRecording()
        {
            if (CurrentRecordingState == RecordingState.Recording)
            {
                CurrentRecordingState = RecordingState.Paused;
                _lastSnapshotRealTime = Time.time;
                DebuggerLog.Info($"{LogPrefix}Recording paused.");
            }
        }

        public void ResumeRecording()
        {
            if (CurrentRecordingState == RecordingState.Paused)
            {
                CurrentRecordingState = RecordingState.Recording;
                _lastSnapshotRealTime = Time.time;
                DebuggerLog.Info($"{LogPrefix}Recording resumed.");
            }
        }

        public void StopRecording()
        {
            if (CurrentRecordingState != RecordingState.Stopped)
            {
                CurrentRecordingState = RecordingState.Stopped;
                _startDelayTimer = 0f;
                _lastSnapshotRealTime = -1f;
                _currentRecordingInfo = null;
                DebuggerLog.Info($"{LogPrefix}Recording stopped.");
            }
        }

        public void Update(float deltaTime)
        {
            if (_currentConfig == null)
                return;

            switch (CurrentRecordingState)
            {
                case RecordingState.Starting:
                    _startDelayTimer -= deltaTime;
                    if (_startDelayTimer <= 0f)
                    {
                        CompleteStart();
                    }
                    break;

                case RecordingState.Recording:

                    break;

                case RecordingState.Paused:

                    break;

                case RecordingState.Stopped:

                    break;
            }
        }

        private void CompleteStart()
        {
            CurrentRecordingState = RecordingState.Recording;

            _currentRecordingInfo = new RecordingStartInfo(Time.time, Time.frameCount);

            DebuggerLog.Info(
                $"{LogPrefix}Recording started at time={_currentRecordingInfo.StartTime:F2}s, frame={_currentRecordingInfo.StartFrame}."
            );

            _lastSnapshotRealTime = _currentRecordingInfo.StartTime;

            _onRecordingStarted?.Invoke(_currentRecordingInfo);
        }

        public bool ShouldTakeSnapshot()
        {
            if (_currentConfig == null || CurrentRecordingState != RecordingState.Recording)
                return false;

            float interval = _currentConfig.UpdateInterval;

            if (Time.time >= _lastSnapshotRealTime + interval)
            {
                _lastSnapshotRealTime = Time.time;

                if (interval <= 0)
                    _lastSnapshotRealTime += 0.016f;
                return true;
            }

            return false;
        }

        public float GetStartDelayTimer() => _startDelayTimer;

        public RecordingStartInfo GetRecordingInfo() => _currentRecordingInfo;
    }
}
