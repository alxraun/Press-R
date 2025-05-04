using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.TabLens.Graphics;
using PressR.Features.TabLens.StorageLens.Commands;
using PressR.Features.TabLens.StorageLens.Core;
using PressR.Features.TabLens.StorageLens.Graphics;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public class StorageLens : ILens
    {
        public string LensId => "StorageLens";
        public string Label => "Storage Lens";

        private readonly IGraphicsManager _graphicsManager;
        private readonly StorageLensThingOverlayGraphicsController _graphicsController;
        private Map _currentMap;
        private readonly TrackedThingsData _trackedThingsData = new TrackedThingsData();
        private StorageSettingsData _storageSettingsData;
        private StorageTabUIData _storageTabUIData;
        private UIStateSnapshot _UIStateSnapshot;
        private SetStorageQuickSearchFromThingCommand.SearchTargetType? _lastHoverFocusType = null;

        public bool IsActive { get; private set; }

        public StorageLens(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _graphicsController = new StorageLensThingOverlayGraphicsController(_graphicsManager);
        }

        public bool TryActivate()
        {
            if (!PressRMod.Settings.tabLensSettings.enableStorageLens)
                return false;

            if (!TryInitializeLensState())
            {
                IsActive = false;
                return false;
            }

            UpdateTrackedThings();
            IsActive = true;
            return true;
        }

        public void Deactivate()
        {
            if (PressRMod.Settings.tabLensSettings.restoreUIStateOnDeactivate)
            {
                RestoreUIState();
            }
            _graphicsController.Clear();
            ClearState();
            IsActive = false;
        }

        public void Update()
        {
            if (!IsValidStateForUpdate())
            {
                IsActive = false;
                _graphicsController.Clear();
                return;
            }

            UpdateTrackedThings();

            if (
                PressRMod.Settings.tabLensSettings.FocusItemInTabOnHover
                && PressRMod.Settings.tabLensSettings.FocusItemInTabOnClick
            )
            {
                HandleMouseHover();
            }

            var context = new StorageLensUpdateContext(_currentMap, _trackedThingsData);
            _graphicsController.Update(context);

            HandleMouseInput();
        }

        private void UpdateTrackedThings()
        {
            if (
                _currentMap == null
                || _storageSettingsData == null
                || !_storageSettingsData.IsValid
            )
            {
                _trackedThingsData.Clear();
                return;
            }

            HashSet<Thing> initialVisibleThings = MapUtils.GetVisibleThingsInViewRectOfType<Thing>(
                _currentMap,
                thing => thing.def.category == ThingCategory.Item
            );

            HashSet<Thing> thingsAllowedByParent = StorageLensHelper.FilterItemsByParentSettings(
                initialVisibleThings,
                _storageSettingsData.SelectedStorage
            );

            Dictionary<Thing, bool> newAllowanceStates =
                StorageLensHelper.CalculateItemAllowanceStates(
                    thingsAllowedByParent,
                    _storageSettingsData.CurrentStorageSettings
                );

            _trackedThingsData.CurrentThings = thingsAllowedByParent;
            _trackedThingsData.AllowanceStates = newAllowanceStates;
        }

        private void HandleMouseHover()
        {
            Thing currentHoveredThing = InputUtils.GetInteractableThingUnderMouse(
                _currentMap,
                thing =>
                    _trackedThingsData.CurrentThings != null
                    && _trackedThingsData.CurrentThings.Contains(thing)
            );
            Thing previousHoveredThing = _trackedThingsData.HoveredThing;

            if (currentHoveredThing == null && previousHoveredThing != null)
            {
                if ((_storageTabUIData?.IsValid ?? false) && _UIStateSnapshot != null)
                {
                    new ClearStorageTabSearchTextCommand(_storageTabUIData).Execute();
                    new SetStorageTabScrollPositionCommand(
                        _storageTabUIData,
                        _UIStateSnapshot.StorageTabScrollPosition
                    ).Execute();
                }
                _lastHoverFocusType = null;
            }
            else if (currentHoveredThing != null)
            {
                var (currentFocusType, _) = StorageLensHelper.GetInteractionTypesFromModifiers();

                bool needsCommandCall =
                    (currentHoveredThing != previousHoveredThing)
                    || (currentFocusType != _lastHoverFocusType);

                if (
                    needsCommandCall
                    && (_storageTabUIData?.IsValid ?? false)
                    && (_storageSettingsData?.IsValid ?? false)
                )
                {
                    if (PressRMod.Settings.tabLensSettings.openStorageTabAutomatically)
                    {
                        new OpenStorageTabCommand(
                            _storageSettingsData.SelectedStorage,
                            _storageTabUIData.Inspector,
                            _storageTabUIData.Selector
                        ).Execute();
                    }

                    new SetStorageQuickSearchFromThingCommand(
                        currentHoveredThing,
                        _storageSettingsData.SelectedStorage,
                        _storageTabUIData,
                        currentFocusType
                    ).Execute();

                    _lastHoverFocusType = currentFocusType;
                }
            }

            _trackedThingsData.HoveredThing = currentHoveredThing;
        }

        private void HandleMouseInput()
        {
            if (!IsActive || !PressRInput.IsMouseButtonDown)
                return;

            Thing clickedThing = InputUtils.GetInteractableThingUnderMouse(
                _currentMap,
                thing =>
                    _trackedThingsData.CurrentThings != null
                    && _trackedThingsData.CurrentThings.Contains(thing)
            );

            Event.current.Use();

            if (clickedThing != null)
            {
                ProcessThingClick(clickedThing);
            }
        }

        private void ProcessThingClick(Thing clickedThing)
        {
            var (focusType, toggleType) = StorageLensHelper.GetInteractionTypesFromModifiers();

            if (
                PressRMod.Settings.tabLensSettings.openStorageTabAutomatically
                && PressRMod.Settings.tabLensSettings.FocusItemInTabOnClick
            )
            {
                new OpenStorageTabCommand(
                    _storageSettingsData.SelectedStorage,
                    _storageTabUIData.Inspector,
                    _storageTabUIData.Selector
                ).Execute();
            }

            if (
                PressRMod.Settings.tabLensSettings.FocusItemInTabOnClick
                && !PressRMod.Settings.tabLensSettings.FocusItemInTabOnHover
            )
            {
                new SetStorageQuickSearchFromThingCommand(
                    clickedThing,
                    _storageSettingsData.SelectedStorage,
                    _storageTabUIData,
                    focusType
                ).Execute();
            }

            new ToggleAllowanceCommand(
                clickedThing,
                _storageSettingsData.CurrentStorageSettings,
                _trackedThingsData,
                toggleType
            ).Execute();
        }

        private bool TryInitializeLensState()
        {
            if (!(Find.Selector.SingleSelectedObject is IStoreSettingsParent selectedStorage))
                return false;

            if (UIUtils.GetActiveITabOfType<ITab_Storage>() == null)
                return false;

            _currentMap = Find.CurrentMap;
            if (_currentMap == null)
                return false;

            var (snapshot, uiData) = StorageLensHelper.FetchUIData();
            if (snapshot == null || uiData == null)
            {
                ClearState();
                return false;
            }
            _UIStateSnapshot = snapshot;
            _storageTabUIData = uiData;

            var currentStorageSettings = selectedStorage.GetStoreSettings();
            if (currentStorageSettings == null)
            {
                ClearState();
                return false;
            }

            _storageSettingsData = new StorageSettingsData(selectedStorage, currentStorageSettings);
            if (!_storageSettingsData.IsValid)
            {
                ClearState();
                return false;
            }

            return true;
        }

        private void RestoreUIState()
        {
            if (_UIStateSnapshot == null)
            {
                return;
            }

            new SetSelectionCommand(_UIStateSnapshot.SelectedObject, _storageTabUIData).Execute();

            new SetOpenTabCommand(
                _UIStateSnapshot.OpenTabType,
                _UIStateSnapshot.Inspector,
                _UIStateSnapshot.Selector
            ).Execute();

            new SetStorageQuickSearchCommand(
                _storageTabUIData.QuickSearchFilter,
                _storageTabUIData.QuickSearchTextProperty,
                _UIStateSnapshot.StorageTabSearchText
            ).Execute();

            new SetStorageTabScrollPositionCommand(
                _storageTabUIData,
                _UIStateSnapshot.StorageTabScrollPosition
            ).Execute();
        }

        private bool IsValidStateForUpdate()
        {
            return IsActive
                && _currentMap != null
                && _graphicsManager != null
                && _storageSettingsData.IsValid
                && _storageTabUIData.IsValid
                && Find.Selector.SingleSelectedObject
                    is IStoreSettingsParent currentlySelectedStorage
                && currentlySelectedStorage == _storageSettingsData.SelectedStorage
                && currentlySelectedStorage.GetStoreSettings()
                    is StorageSettings currentStorageSettings
                && currentStorageSettings == _storageSettingsData.CurrentStorageSettings;
        }

        private void ClearState()
        {
            _currentMap = null;
            _trackedThingsData.Clear();
            _storageSettingsData = null;
            _storageTabUIData = null;
            _UIStateSnapshot = null;
            _lastHoverFocusType = null;
        }
    }
}
