using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace PressR.Debugger
{
    public enum DebuggerLogLevel
    {
        Info,
        Warning,
        Error,
    }

    public struct DebuggerLogEntry
    {
        public float Timestamp { get; }
        public DebuggerLogLevel Level { get; }
        public string Message { get; }

        public DebuggerLogEntry(float timestamp, DebuggerLogLevel level, string message)
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

    public static class DebuggerLog
    {
        private static readonly List<DebuggerLogEntry> _logEntries = new List<DebuggerLogEntry>();
        private static readonly int MaxLogEntries = 500;

        public static IEnumerable<DebuggerLogEntry> GetLogs()
        {
            lock (_logEntries)
            {
                return new List<DebuggerLogEntry>(_logEntries);
            }
        }

        public static void Info(string message)
        {
            AddEntry(DebuggerLogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            AddEntry(DebuggerLogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            AddEntry(DebuggerLogLevel.Error, message);
        }

        private static void AddEntry(DebuggerLogLevel level, string message)
        {
            lock (_logEntries)
            {
                if (_logEntries.Count >= MaxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
                float timestamp = UnityData.IsInMainThread ? Time.time : -1f;
                _logEntries.Add(new DebuggerLogEntry(timestamp, level, message));
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
