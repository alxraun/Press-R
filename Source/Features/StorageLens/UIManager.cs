using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Microtools.Features.StorageLens
{
    public class UIManager(State state)
    {
        private readonly State _state = state;

        public bool CaptureUIState()
        {
            var selector = Find.Selector;
            if (selector is null)
            {
                return false;
            }

            var inspectPane = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
            if (inspectPane is null)
            {
                return false;
            }

            _state.UISnapshot_SelectedObject = selector.SingleSelectedObject;
            _state.UISnapshot_OpenTabType = inspectPane.OpenTabType;

            _state.Selector = selector;
            _state.Inspector = inspectPane;

            if (TryGetStorageTabQuickSearchMembers())
            {
                _state.UISnapshot_StorageTabSearchText = GetPropertyValue<string>(
                    _state.QuickSearchFilter,
                    "Text"
                );
                _state.UISnapshot_StorageTabScrollPosition = GetStorageTabScrollPosition();
                return true;
            }

            return false;
        }

        private bool TryGetStorageTabQuickSearchMembers()
        {
            var inspectPane = _state.Inspector;
            if (inspectPane == null)
            {
                return false;
            }
            var storageTab = inspectPane.CurTabs?.FirstOrDefault(t => t is ITab_Storage);
            if (storageTab == null)
            {
                return false;
            }
            var thingFilterState = GetFieldValue<object>(storageTab, "thingFilterState");
            if (thingFilterState == null)
            {
                return false;
            }
            _state.ThingFilterState = thingFilterState;
            var quickSearchWidget = GetFieldValue<object>(thingFilterState, "quickSearch");
            if (quickSearchWidget == null)
            {
                return false;
            }
            var quickSearchFilter = GetFieldValue<object>(quickSearchWidget, "filter");
            if (quickSearchFilter == null)
            {
                return false;
            }
            var quickSearchTextProperty = GetPropertyInfo(quickSearchFilter, "Text");
            if (quickSearchTextProperty == null)
            {
                return false;
            }
            _state.QuickSearchFilter = quickSearchFilter;
            _state.QuickSearchTextProperty = quickSearchTextProperty;
            return true;
        }

        private Vector2 GetStorageTabScrollPosition()
        {
            if (_state.ThingFilterState == null)
            {
                return Vector2.zero;
            }

            return GetFieldValue<Vector2>(_state.ThingFilterState, "scrollPosition");
        }

        private static T GetFieldValue<T>(
            object obj,
            string fieldName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
            {
                return default;
            }

            var field = obj.GetType().GetField(fieldName, bindingFlags);
            return field != null ? (T)field.GetValue(obj) : default;
        }

        private static T GetPropertyValue<T>(
            object obj,
            string propertyName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
            {
                return default;
            }

            var property = obj.GetType().GetProperty(propertyName, bindingFlags);
            return property != null ? (T)property.GetValue(obj) : default;
        }

        private static PropertyInfo GetPropertyInfo(
            object obj,
            string propertyName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
            {
                return null;
            }

            return obj.GetType().GetProperty(propertyName, bindingFlags);
        }
    }
}
