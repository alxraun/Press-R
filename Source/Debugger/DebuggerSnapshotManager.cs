using System.Collections.Generic;
using System.Linq;
using PressR.Debugger.Resolver;
using UnityEngine;
using Verse;

namespace PressR.Debugger
{
    public class DebuggerSnapshotManager
    {
        public int MaxHistorySize { get; set; } = 1000;
        public List<Dictionary<string, object>> SnapshotsHistory { get; private set; } =
            new List<Dictionary<string, object>>();
        public Dictionary<string, object> LastSnapshot { get; private set; } =
            new Dictionary<string, object>();

        private readonly ValueResolver _valueResolver;
        private List<DebuggerTrackedValueInfo> _currentTrackedValues =
            new List<DebuggerTrackedValueInfo>();

        private RecordingStartInfo _recordingStartInfo;

        public DebuggerSnapshotManager(ValueResolver valueResolver)
        {
            _valueResolver =
                valueResolver ?? throw new System.ArgumentNullException(nameof(valueResolver));
        }

        public void SetTrackedValues(List<DebuggerTrackedValueInfo> trackedValues)
        {
            _currentTrackedValues = trackedValues ?? new List<DebuggerTrackedValueInfo>();

            LastSnapshot = _currentTrackedValues.ToDictionary(
                tvi => tvi.DisplayName,
                tvi => (object)"-"
            );
        }

        public void SetRecordingStartInfo(RecordingStartInfo startInfo)
        {
            _recordingStartInfo = startInfo;
        }

        public void ClearHistory()
        {
            SnapshotsHistory.Clear();
        }

        public void StoreLastSnapshot()
        {
            if (LastSnapshot != null && LastSnapshot.Any())
            {
                SnapshotsHistory.Add(new Dictionary<string, object>(LastSnapshot));
                while (SnapshotsHistory.Count > MaxHistorySize && MaxHistorySize > 0)
                {
                    SnapshotsHistory.RemoveAt(0);
                }
            }
        }

        public void TakeSnapshot()
        {
            if (_currentTrackedValues == null || !_currentTrackedValues.Any())
            {
                if (LastSnapshot.Any())
                    LastSnapshot =
                        _currentTrackedValues?.ToDictionary(
                            tvi => tvi.DisplayName,
                            tvi => (object)"-"
                        ) ?? new Dictionary<string, object>();
                return;
            }

            var currentSnapshot = new Dictionary<string, object>();

            if (_recordingStartInfo != null)
            {
                currentSnapshot["Frame"] = Time.frameCount - _recordingStartInfo.StartFrame;
                currentSnapshot["Time"] = Time.time - _recordingStartInfo.StartTime;
            }
            else
            {
                currentSnapshot["Frame"] = Time.frameCount;
                currentSnapshot["Time"] = Time.time;
            }

            foreach (var tvi in _currentTrackedValues)
            {
                ValueResolutionResult resolutionResult = _valueResolver.Resolve(tvi.Path);
                object snapshotValue;

                if (resolutionResult.IsSuccess)
                {
                    snapshotValue = DebuggerValueFormatter.FormatValue(resolutionResult.Value, tvi);
                }
                else
                {
                    snapshotValue = $"Error: {resolutionResult.Error}";
                }

                currentSnapshot[tvi.DisplayName] = snapshotValue;
            }

            LastSnapshot = currentSnapshot;
        }
    }
}
