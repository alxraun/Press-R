using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace PressR.Debugger
{
    public static class DebuggerValueFormatter
    {
        private const string LogPrefix = "[Debugger] ";

        public static string FormatValue(object rawValue, DebuggerTrackedValueInfo tvi)
        {
            if (rawValue == null)
            {
                return "null";
            }

            switch (tvi.CollectionDisplay)
            {
                case CollectionDisplayMode.CountOnly:
                    if (rawValue is ICollection collCount)
                        return collCount.Count.ToString();
                    else if (rawValue is IDictionary dictCount)
                        return dictCount.Count.ToString();
                    else
                        return rawValue.ToString();

                case CollectionDisplayMode.ShowItems:
                    if (rawValue is ICollection collItems)
                        return FormatCollectionItems(collItems, tvi.ItemLimit);
                    else
                        return "Value is not ICollection";

                case CollectionDisplayMode.ShowKeys:
                    if (rawValue is IDictionary dictKeys)
                        return FormatDictionaryKeys(dictKeys, tvi.ItemLimit);
                    else
                        return "Value is not IDictionary";

                case CollectionDisplayMode.ValueToString:
                default:
                    return rawValue.ToString();
            }
        }

        private static string FormatDictionaryKeys(IDictionary dictionary, int? itemLimit)
        {
            var sb = new StringBuilder();
            sb.Append($"Count: {dictionary.Count} [Keys: ");
            int count = 0;
            bool limitReached = false;
            foreach (var key in dictionary.Keys)
            {
                if (itemLimit.HasValue && count >= itemLimit.Value)
                {
                    limitReached = true;
                    break;
                }
                if (count > 0)
                    sb.Append(", ");
                try
                {
                    sb.Append(key?.ToString() ?? "null");
                }
                catch (Exception ex)
                {
                    sb.Append("?Err?");
                    DebuggerLog.Warning(
                        $"{LogPrefix}Error formatting dictionary key: {ex.Message}"
                    );
                }
                count++;
            }
            if (limitReached)
                sb.Append(", ...");
            sb.Append("]");
            return sb.ToString();
        }

        private static string FormatCollectionItems(ICollection collection, int? itemLimit)
        {
            var sb = new StringBuilder();
            sb.Append($"Count: {collection.Count} [Items: ");
            int count = 0;
            bool limitReached = false;
            foreach (var item in collection)
            {
                if (itemLimit.HasValue && count >= itemLimit.Value)
                {
                    limitReached = true;
                    break;
                }
                if (count > 0)
                    sb.Append(", ");
                try
                {
                    sb.Append(item?.ToString() ?? "null");
                }
                catch (Exception ex)
                {
                    sb.Append("?Err?");
                    DebuggerLog.Warning(
                        $"{LogPrefix}Error formatting collection item: {ex.Message}"
                    );
                }
                count++;
            }
            if (limitReached)
                sb.Append(", ...");
            sb.Append("]");
            return sb.ToString();
        }
    }
}
