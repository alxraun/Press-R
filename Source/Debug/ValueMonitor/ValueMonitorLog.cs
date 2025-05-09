using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace PressR.Debug.ValueMonitor
{
    public enum ValueMonitorLogLevel
    {
        Info,
        Warning,
        Error,
    }

    public struct ValueMonitorLogEntry
    {
        public float Timestamp { get; }
        public ValueMonitorLogLevel Level { get; }
        public string Message { get; }

        public ValueMonitorLogEntry(float timestamp, ValueMonitorLogLevel level, string message)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            string timestampStr = Timestamp < 0 ? "??.??s" : $"{Timestamp:F2}s";
            return $"[{timestampStr}][{Level}] {Message}";
        }
    }

    public static class ValueMonitorLog
    {
        private static readonly List<ValueMonitorLogEntry> _logEntries =
            new List<ValueMonitorLogEntry>();
        private static readonly int MaxLogEntries = 500;

        public static IEnumerable<ValueMonitorLogEntry> GetLogs()
        {
            lock (_logEntries)
            {
                return new List<ValueMonitorLogEntry>(_logEntries);
            }
        }

        public static void Info(string message)
        {
            AddEntry(ValueMonitorLogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            AddEntry(ValueMonitorLogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            AddEntry(ValueMonitorLogLevel.Error, message);
        }

        private static void AddEntry(ValueMonitorLogLevel level, string message)
        {
            lock (_logEntries)
            {
                if (_logEntries.Count >= MaxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
                float timestamp = UnityData.IsInMainThread ? Time.time : -1f;
                _logEntries.Add(new ValueMonitorLogEntry(timestamp, level, message));
            }
        }

        public static void ClearLogs()
        {
            lock (_logEntries)
            {
                _logEntries.Clear();
            }
            Info("Logs cleared.");
        }

        public static string GetLogsAsString()
        {
            var sb = new StringBuilder();
            lock (_logEntries)
            {
                foreach (var entry in _logEntries)
                {
                    sb.AppendLine(entry.ToString());
                }
            }
            return sb.ToString();
        }
    }
}
