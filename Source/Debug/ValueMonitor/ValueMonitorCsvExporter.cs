using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PressR.Debug.ValueMonitor
{
    public static class ValueMonitorCsvExporter
    {
        public static string GetHistoryAsCsv(
            List<Dictionary<string, object>> snapshotsHistory,
            List<ValueMonitorTrackedValueInfo> trackedValues
        )
        {
            if (snapshotsHistory == null || !snapshotsHistory.Any())
            {
                return "No data recorded.";
            }

            var sb = new StringBuilder();

            var headers = new List<string> { "Frame", "Time" };
            var valueKeys = new List<string> { "Frame", "Time" };

            if (trackedValues != null)
            {
                foreach (var tvi in trackedValues)
                {
                    if (tvi.DisplayName != "Frame" && tvi.DisplayName != "Time")
                    {
                        headers.Add(tvi.DisplayName);
                        valueKeys.Add(tvi.DisplayName);
                    }
                }
            }
            else
            {
                var firstSnapshotKeys = snapshotsHistory
                    .First()
                    .Keys.Where(k => k != "Frame" && k != "Time")
                    .ToList();
                headers.AddRange(firstSnapshotKeys);
                valueKeys.AddRange(firstSnapshotKeys);
            }

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvValue)));

            foreach (var snapshot in snapshotsHistory)
            {
                var rowValues = new List<string>();
                foreach (var key in valueKeys)
                {
                    snapshot.TryGetValue(key, out var value);
                    string formattedValue;

                    if (key == "Time" && value is float timeValue)
                    {
                        formattedValue = timeValue.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        formattedValue = value?.ToString() ?? "";
                    }

                    rowValues.Add(EscapeCsvValue(formattedValue));
                }
                sb.AppendLine(string.Join(",", rowValues));
            }

            return sb.ToString();
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (
                value.Contains(",")
                || value.Contains("\"")
                || value.Contains("\n")
                || value.Contains("\r")
            )
            {
                var escapedValue = value.Replace("\"", "\"\"");
                return $"\"{escapedValue}\"";
            }
            return value;
        }
    }
}
