using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Microtools.Features.StorageLens
{
    public class ActionDispatcher(State state)
    {
        private readonly State _state = state;

        public enum AllowanceToggleType
        {
            Item,
            Category,
            ParentCategory,
            All,
        }

        public enum SearchTargetType
        {
            Item,
            Category,
            ParentCategory,
            Clear,
        }

        public void ToggleAllowance(Thing thing, AllowanceToggleType toggleType)
        {
            if (_state.SelectedStorage?.GetStoreSettings() == null || thing == null)
            {
                return;
            }

            switch (toggleType)
            {
                case AllowanceToggleType.Item:
                    ToggleItem(thing);
                    break;
                case AllowanceToggleType.Category:
                    ToggleCategory(thing);
                    break;
                case AllowanceToggleType.ParentCategory:
                    ToggleParentCategory(thing);
                    break;
                case AllowanceToggleType.All:
                    ToggleAll(thing);
                    break;
            }
        }

        private void ToggleItem(Thing thing)
        {
            if (thing?.def == null)
            {
                return;
            }

            var filter = _state.SelectedStorage.GetStoreSettings().filter;
            var def = thing.def;

            _state.AllowanceStatesForSelectedStorage.TryGetValue(def, out var isAllowed);
            var newState = !isAllowed;

            filter.SetAllow(def, newState);

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleCategory(Thing thing)
        {
            var category = thing.def.FirstThingCategory;
            if (category == null)
            {
                ToggleItem(thing);
                return;
            }

            var filter = _state.SelectedStorage.GetStoreSettings().filter;
            _state.AllowanceStatesForSelectedStorage.TryGetValue(thing.def, out var isAllowed);
            var newState = !isAllowed;

            filter.SetAllow(category, newState);

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleParentCategory(Thing thing)
        {
            var firstLevelCategory = thing.def.FirstThingCategory;
            if (firstLevelCategory == null)
            {
                ToggleItem(thing);
                return;
            }

            var categoryToToggle = firstLevelCategory.parent;
            if (categoryToToggle == null || categoryToToggle == ThingCategoryDefOf.Root)
            {
                categoryToToggle = firstLevelCategory;
            }

            var filter = _state.SelectedStorage.GetStoreSettings().filter;
            _state.AllowanceStatesForSelectedStorage.TryGetValue(thing.def, out var isAllowed);
            var newState = !isAllowed;

            filter.SetAllow(categoryToToggle, newState);

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleAll(Thing thing)
        {
            var filter = _state.SelectedStorage.GetStoreSettings().filter;
            _state.AllowanceStatesForSelectedStorage.TryGetValue(thing.def, out var isAllowed);
            var newState = !isAllowed;

            if (newState)
            {
                var parentFilter = _state.SelectedStorage?.GetParentStoreSettings()?.filter;
                filter.SetAllowAll(parentFilter);
            }
            else
            {
                filter.SetDisallowAll();
            }

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        public void SetSearchTextFromThing(Thing thing, SearchTargetType focusType)
        {
            if (focusType == SearchTargetType.Clear)
            {
                ClearSearchText();
                return;
            }

            var searchText = GetSearchTextForThing(thing, focusType);
            if (searchText != null)
            {
                SetSearchText(searchText);
            }
        }

        private string GetSearchTextForThing(Thing thing, SearchTargetType focusType)
        {
            if (thing?.def == null)
            {
                return null;
            }

            var firstCategory = thing.def.FirstThingCategory;

            switch (focusType)
            {
                case SearchTargetType.Item:
                    return thing.def.LabelCap;
                case SearchTargetType.Category:
                    return firstCategory != null ? firstCategory.LabelCap : thing.def.LabelCap;
                case SearchTargetType.ParentCategory:
                    if (
                        firstCategory?.parent != null
                        && firstCategory.parent != ThingCategoryDefOf.Root
                    )
                    {
                        return firstCategory.parent.LabelCap;
                    }

                    return firstCategory?.LabelCap ?? thing.def.LabelCap;
                default:
                    return null;
            }
        }

        public void SetSearchText(string text)
        {
            if (_state.QuickSearchTextProperty != null && _state.QuickSearchFilter != null)
            {
                _state.QuickSearchTextProperty.SetValue(_state.QuickSearchFilter, text);
            }
        }

        public void ClearSearchText()
        {
            SetSearchText("");
        }

        public void OpenStorageTab()
        {
            if (
                _state.SelectedStorage == null
                || _state.Inspector == null
                || _state.Selector == null
            )
            {
                return;
            }

            var selector = _state.Selector;

            if (
                selector.SingleSelectedObject != _state.SelectedStorage
                && _state.SelectedStorage is Thing storageParentThing
            )
            {
                selector.ClearSelection();
                selector.Select(storageParentThing);
            }

            SetOpenTab(typeof(ITab_Storage));
        }

        public void SetOpenTab(Type targetTabType)
        {
            if (_state.Inspector == null || _state.Selector == null)
            {
                return;
            }

            var currentTabType = _state.Inspector.OpenTabType;

            if (targetTabType == null)
            {
                if (currentTabType != null)
                {
                    _state.Inspector.CloseOpenTab();
                }

                return;
            }

            var currentSelection = _state.Selector.SingleSelectedObject;

            if (currentSelection == null)
            {
                if (currentTabType != null)
                {
                    _state.Inspector.CloseOpenTab();
                }

                return;
            }

            var targetTabInstance = _state.Inspector.CurTabs?.FirstOrDefault(t =>
                t.GetType() == targetTabType
            );

            if (targetTabInstance != null)
            {
                if (currentTabType != targetTabType)
                {
                    InspectPaneUtility.OpenTab(targetTabType);
                }
            }
            else
            {
                if (currentTabType != null)
                {
                    _state.Inspector.CloseOpenTab();
                }
            }
        }

        public void SetSelection(object target)
        {
            if (_state.Selector == null)
            {
                return;
            }

            var selector = _state.Selector;
            var currentSelection = selector.SingleSelectedObject;

            if (target == currentSelection)
            {
                return;
            }

            if (target == null)
            {
                selector.ClearSelection();
                return;
            }

            var canSelect = true;
            if (target is Thing thing && (thing.Destroyed || !thing.Spawned))
            {
                canSelect = false;
            }

            if (canSelect)
            {
                selector.Select(target);
            }
            else
            {
                selector.ClearSelection();
            }
        }

        public void SetStorageTabScrollPosition(Vector2 position)
        {
            if (_state.ThingFilterState == null)
            {
                return;
            }

            var scrollPositionField = _state
                .ThingFilterState.GetType()
                .GetField(
                    "scrollPosition",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

            scrollPositionField?.SetValue(_state.ThingFilterState, position);
        }

        private void NotifySettingsChanged()
        {
            _state.SelectedStorage?.Notify_SettingsChanged();
        }

        private void PlayToggleSound(bool turnedOn)
        {
            if (turnedOn)
            {
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
        }
    }
}
