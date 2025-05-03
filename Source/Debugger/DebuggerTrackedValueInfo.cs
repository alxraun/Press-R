using System;
using System.Collections.Generic;

namespace PressR.Debugger
{
    public enum CollectionDisplayMode
    {
        ValueToString,
        CountOnly,
        ShowItems,
        ShowKeys,
    }

    public class DebuggerTrackedValueInfo
    {
        private const int DefaultItemLimit = 3;

        public string Path { get; }
        public string DisplayName { get; }
        public CollectionDisplayMode CollectionDisplay { get; }
        public int? ItemLimit { get; }

        private DebuggerTrackedValueInfo(
            string path,
            string displayName,
            CollectionDisplayMode collectionDisplay,
            int? itemLimit
        )
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            DisplayName = string.IsNullOrEmpty(displayName) ? path : displayName;
            CollectionDisplay = collectionDisplay;
            ItemLimit = itemLimit;
        }

        public static DebuggerTrackedValueInfo TrackValue(string path, string displayName = null)
        {
            return new DebuggerTrackedValueInfo(
                path,
                displayName,
                CollectionDisplayMode.ValueToString,
                null
            );
        }

        public static DebuggerTrackedValueInfo TrackCollectionCount(
            string path,
            string displayName = null
        )
        {
            return new DebuggerTrackedValueInfo(
                path,
                displayName,
                CollectionDisplayMode.CountOnly,
                null
            );
        }

        public static CollectionTrackingConfigurator TrackCollectionItems(
            string path,
            string displayName = null
        )
        {
            return new CollectionTrackingConfigurator(
                path,
                displayName,
                CollectionDisplayMode.ShowItems
            );
        }

        public static CollectionTrackingConfigurator TrackDictionaryKeys(
            string path,
            string displayName = null
        )
        {
            return new CollectionTrackingConfigurator(
                path,
                displayName,
                CollectionDisplayMode.ShowKeys
            );
        }

        public readonly struct CollectionTrackingConfigurator
        {
            private readonly string _path;
            private readonly string _displayName;
            private readonly CollectionDisplayMode _collectionDisplay;
            private readonly int? _itemLimit;

            internal CollectionTrackingConfigurator(
                string path,
                string displayName,
                CollectionDisplayMode collectionDisplay
            )
            {
                _path = path;
                _displayName = displayName;
                _collectionDisplay = collectionDisplay;
                _itemLimit = DefaultItemLimit;
            }

            private CollectionTrackingConfigurator(
                string path,
                string displayName,
                CollectionDisplayMode collectionDisplay,
                int? itemLimit
            )
            {
                _path = path;
                _displayName = displayName;
                _collectionDisplay = collectionDisplay;
                _itemLimit = itemLimit;
            }

            public CollectionTrackingConfigurator WithLimit(int limit)
            {
                return new CollectionTrackingConfigurator(
                    _path,
                    _displayName,
                    _collectionDisplay,
                    limit > 0 ? limit : DefaultItemLimit
                );
            }

            public CollectionTrackingConfigurator ShowAll()
            {
                return new CollectionTrackingConfigurator(
                    _path,
                    _displayName,
                    _collectionDisplay,
                    null
                );
            }

            public static implicit operator DebuggerTrackedValueInfo(
                CollectionTrackingConfigurator configurator
            )
            {
                return new DebuggerTrackedValueInfo(
                    configurator._path,
                    configurator._displayName,
                    configurator._collectionDisplay,
                    configurator._itemLimit
                );
            }
        }

        public static class ValuePathSyntax
        {
            public const string StandardPathExample = "Find.CurrentMap.mapPawns.AllPawns";
            public const string SimpleTypeExample = "TickManager.TicksGame";
            public const string AssemblyQualifiedExample =
                "Namespace.TypeName, AssemblyName.StaticMember";
            public const string KnownTypesExample = "Current, Find, Game, UnityEngine, etc.";
            public const string UnsupportedSyntaxNote =
                "Indexers [...], conditional access ?., method calls (...) are not supported";

            public static string FormatExamplesForHelp()
            {
                return $"Supported path formats:\n"
                    + $"- Standard dotted path: {StandardPathExample}\n"
                    + $"- Simple type path (try common namespaces): {SimpleTypeExample}\n"
                    + $"- Assembly qualified name (if needed): {AssemblyQualifiedExample}\n"
                    + $"Known static entry points: {KnownTypesExample}\n"
                    + $"Note: {UnsupportedSyntaxNote}";
            }
        }
    }
}
