using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PressR.Features.TabLens.StorageLens.Commands;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public class StorageLensState
    {
        public HashSet<Thing> CurrentThings { get; internal set; } = new HashSet<Thing>();
        public Dictionary<Thing, bool> AllowanceStates { get; internal set; } =
            new Dictionary<Thing, bool>();
        public Thing HoveredThing { get; internal set; } = null;

        public IStoreSettingsParent SelectedStorage { get; internal set; }
        public StorageSettings CurrentStorageSettings { get; internal set; }

        public MainTabWindow_Inspect Inspector { get; internal set; }
        public Selector Selector { get; internal set; }

        public object QuickSearchWidget { get; internal set; }
        public object QuickSearchFilter { get; internal set; }
        public PropertyInfo QuickSearchTextProperty { get; internal set; }
        public object ThingFilterState { get; internal set; }

        public object UISnapshot_SelectedObject { get; internal set; }
        public Type UISnapshot_OpenTabType { get; internal set; }
        public string UISnapshot_StorageTabSearchText { get; internal set; }
        public Vector2 UISnapshot_StorageTabScrollPosition { get; internal set; }

        public Map CurrentMap { get; internal set; }
        public SetStorageQuickSearchFromThingCommand.SearchTargetType? LastHoverFocusType
        {
            get;
            internal set;
        } = null;

        public bool IsFullyInitialized =>
            CurrentMap != null
            && SelectedStorage != null
            && CurrentStorageSettings != null
            && Inspector != null
            && Selector != null
            && HasStorageTabUIData
            && HasUISnapshotData;
        public bool HasStorageSettings => SelectedStorage != null && CurrentStorageSettings != null;

        public bool HasStorageTabUIData =>
            QuickSearchWidget != null
            && QuickSearchFilter != null
            && QuickSearchTextProperty != null
            && ThingFilterState != null
            && Inspector != null
            && Selector != null;

        public bool HasUISnapshotData => Inspector != null && Selector != null;

        public void ClearTrackedThingsData()
        {
            CurrentThings.Clear();
            AllowanceStates.Clear();
            HoveredThing = null;
        }

        public bool GetAllowanceState(Thing thing)
        {
            return AllowanceStates != null
                && AllowanceStates.TryGetValue(thing, out bool isAllowed)
                && isAllowed;
        }

        public void ClearAllState()
        {
            ClearTrackedThingsData();

            SelectedStorage = null;
            CurrentStorageSettings = null;

            QuickSearchWidget = null;
            QuickSearchFilter = null;
            QuickSearchTextProperty = null;
            Inspector = null;
            Selector = null;
            ThingFilterState = null;

            UISnapshot_SelectedObject = null;
            UISnapshot_OpenTabType = null;
            UISnapshot_StorageTabSearchText = null;
            UISnapshot_StorageTabScrollPosition = Vector2.zero;

            CurrentMap = null;
            LastHoverFocusType = null;
        }
    }
}
