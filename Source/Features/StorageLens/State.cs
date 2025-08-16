using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.StorageLens
{
    public class State
    {
        public Map CurrentMap;
        public IStoreSettingsParent SelectedStorage;

        public HashSet<Thing> StorableForSelectedStorageInView = [];
        public Dictionary<ThingDef, bool> AllowanceStatesForSelectedStorage = [];

        public MainTabWindow_Inspect Inspector;
        public Selector Selector;
        public object QuickSearchWidget;
        public object QuickSearchFilter;
        public PropertyInfo QuickSearchTextProperty;
        public object ThingFilterState;

        public object UISnapshot_SelectedObject;
        public Type UISnapshot_OpenTabType;
        public string UISnapshot_StorageTabSearchText;
        public Vector2 UISnapshot_StorageTabScrollPosition;

        public void Clear()
        {
            CurrentMap = null;
            SelectedStorage = null;
            StorableForSelectedStorageInView.Clear();
            AllowanceStatesForSelectedStorage.Clear();
            Inspector = null;
            Selector = null;
            QuickSearchWidget = null;
            QuickSearchFilter = null;
            QuickSearchTextProperty = null;
            ThingFilterState = null;
            UISnapshot_SelectedObject = null;
            UISnapshot_OpenTabType = null;
            UISnapshot_StorageTabSearchText = null;
            UISnapshot_StorageTabScrollPosition = Vector2.zero;
        }
    }
}
