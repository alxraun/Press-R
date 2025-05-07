using System;
using System.Linq;
using System.Reflection;
using PressR.Features.TabLens.StorageLens.Commands;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public class StorageLensUIManager
    {
        public void FetchAndSaveCurrentUIState(StorageLensState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            state.QuickSearchWidget = null;
            state.QuickSearchFilter = null;
            state.QuickSearchTextProperty = null;
            state.ThingFilterState = null;

            state.UISnapshot_StorageTabSearchText = null;
            state.UISnapshot_StorageTabScrollPosition = Vector2.zero;

            state.Selector = Find.Selector;
            if (state.Selector is null)
                return;

            state.Inspector = MainButtonDefOf.Inspect.TabWindow as MainTabWindow_Inspect;
            if (state.Inspector is null)
                return;

            state.UISnapshot_SelectedObject = state.Selector.SingleSelectedObject;
            state.UISnapshot_OpenTabType = state.Inspector.OpenTabType;

            ITab_Storage storageTabInstance = state
                .Inspector.CurTabs?.OfType<ITab_Storage>()
                .FirstOrDefault();
            if (storageTabInstance is null)
                return;

            state.ThingFilterState = GetFieldValue<object>(storageTabInstance, "thingFilterState");
            if (state.ThingFilterState is null)
                return;

            state.QuickSearchWidget = GetFieldValue<object>(state.ThingFilterState, "quickSearch");
            if (state.QuickSearchWidget is null)
                return;

            state.QuickSearchFilter = GetFieldValue<object>(state.QuickSearchWidget, "filter");
            if (state.QuickSearchFilter is null)
                return;

            state.QuickSearchTextProperty = GetPropertyInfo(state.QuickSearchFilter, "Text");
            if (state.QuickSearchTextProperty is null)
                return;

            state.UISnapshot_StorageTabScrollPosition = GetFieldValue<Vector2>(
                state.ThingFilterState,
                "scrollPosition"
            );

            state.UISnapshot_StorageTabSearchText = GetPropertyValue<string>(
                state.QuickSearchFilter,
                "Text"
            );
        }

        public void RestorePreviousUIState(StorageLensState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!state.HasUISnapshotData)
            {
                return;
            }

            new SetSelectionCommand(state.UISnapshot_SelectedObject, state).Execute();

            new SetOpenTabCommand(
                state.UISnapshot_OpenTabType,
                state.Inspector,
                state.Selector
            ).Execute();

            new SetStorageQuickSearchCommand(
                state,
                state.UISnapshot_StorageTabSearchText
            ).Execute();

            new SetStorageTabScrollPositionCommand(
                state,
                state.UISnapshot_StorageTabScrollPosition
            ).Execute();
        }

        private T GetFieldValue<T>(
            object obj,
            string fieldName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
                return default(T);
            var field = obj.GetType().GetField(fieldName, bindingFlags);
            return field != null ? (T)field.GetValue(obj) : default(T);
        }

        private T GetPropertyValue<T>(
            object obj,
            string propertyName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
                return default(T);
            var property = obj.GetType().GetProperty(propertyName, bindingFlags);
            return property != null ? (T)property.GetValue(obj) : default;
        }

        private PropertyInfo GetPropertyInfo(
            object obj,
            string propertyName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
                return null;
            return obj.GetType().GetProperty(propertyName, bindingFlags);
        }
    }
}
