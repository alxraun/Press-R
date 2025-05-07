using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.TabLens.Graphics;
using PressR.Features.TabLens.StorageLens.Commands;
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
        private readonly StorageLensState _state;
        private readonly StorageLensThingsProvider _thingsProvider;
        private readonly StorageLensUIManager _storageLensUIManager;

        public bool IsActive { get; private set; }

        public StorageLens(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _state = new StorageLensState();
            _graphicsController = new StorageLensThingOverlayGraphicsController(
                _graphicsManager,
                _state
            );
            _thingsProvider = new StorageLensThingsProvider();
            _storageLensUIManager = new StorageLensUIManager();
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
                _storageLensUIManager.RestorePreviousUIState(_state);
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

            if (PressRMod.Settings.tabLensSettings.FocusItemInTabOnHover)
            {
                HandleMouseHover();
            }

            _graphicsController.Update();

            HandleMouseInput();
        }

        private void UpdateTrackedThings()
        {
            if (
                _state.CurrentMap == null
                || _state.SelectedStorage == null
                || !_state.HasStorageSettings
            )
            {
                _state.ClearTrackedThingsData();
                return;
            }

            HashSet<Thing> initialVisibleThings = _thingsProvider.GetVisibleItemsInViewRect(
                _state.CurrentMap
            );

            HashSet<Thing> thingsAllowedByParent = StorageLensHelper.FilterItemsByParentSettings(
                initialVisibleThings,
                _state.SelectedStorage
            );

            Dictionary<Thing, bool> newAllowanceStates =
                StorageLensHelper.CalculateItemAllowanceStates(
                    thingsAllowedByParent,
                    _state.CurrentStorageSettings
                );

            _state.CurrentThings = thingsAllowedByParent;
            _state.AllowanceStates = newAllowanceStates;
        }

        private void HandleMouseHover()
        {
            Thing currentHoveredThing = InputUtils.GetInteractableThingUnderMouse(
                _state.CurrentMap,
                thing => _state.CurrentThings != null && _state.CurrentThings.Contains(thing)
            );
            Thing previousHoveredThing = _state.HoveredThing;

            if (currentHoveredThing == null && previousHoveredThing != null)
            {
                if (
                    _state.HasStorageTabUIData
                    && _state.HasUISnapshotData
                    && _state.Inspector != null
                    && Find.WindowStack.CurrentWindowGetsInput
                )
                {
                    new ClearStorageTabSearchTextCommand(_state).Execute();
                    new SetStorageTabScrollPositionCommand(
                        _state,
                        _state.UISnapshot_StorageTabScrollPosition
                    ).Execute();
                }
                _state.LastHoverFocusType = null;
            }
            else if (currentHoveredThing != null)
            {
                var (currentFocusType, _) = StorageLensHelper.GetInteractionTypesFromModifiers();

                bool needsCommandCall =
                    (currentHoveredThing != previousHoveredThing)
                    || (currentFocusType != _state.LastHoverFocusType);

                if (needsCommandCall && _state.HasStorageTabUIData && _state.HasStorageSettings)
                {
                    if (PressRMod.Settings.tabLensSettings.openStorageTabAutomatically)
                    {
                        new OpenStorageTabCommand(
                            _state.SelectedStorage,
                            _state.Inspector,
                            _state.Selector
                        ).Execute();
                    }

                    new SetStorageQuickSearchFromThingCommand(
                        currentHoveredThing,
                        _state,
                        currentFocusType
                    ).Execute();

                    _state.LastHoverFocusType = currentFocusType;
                }
            }

            _state.HoveredThing = currentHoveredThing;
        }

        private void HandleMouseInput()
        {
            if (!IsActive || !PressRInput.IsMouseButtonDown)
                return;

            Thing clickedThing = InputUtils.GetInteractableThingUnderMouse(
                _state.CurrentMap,
                thing => _state.CurrentThings != null && _state.CurrentThings.Contains(thing)
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
                && _state.HasStorageSettings
                && _state.Inspector != null
                && _state.Selector != null
            )
            {
                new OpenStorageTabCommand(
                    _state.SelectedStorage,
                    _state.Inspector,
                    _state.Selector
                ).Execute();
            }

            if (
                PressRMod.Settings.tabLensSettings.FocusItemInTabOnClick
                && !PressRMod.Settings.tabLensSettings.FocusItemInTabOnHover
                && _state.HasStorageSettings
                && _state.HasStorageTabUIData
            )
            {
                new SetStorageQuickSearchFromThingCommand(
                    clickedThing,
                    _state,
                    focusType
                ).Execute();
            }

            if (_state.HasStorageSettings)
            {
                new ToggleAllowanceCommand(clickedThing, _state, toggleType).Execute();
            }
        }

        private bool TryInitializeLensState()
        {
            _state.ClearAllState();

            if (!(Find.Selector.SingleSelectedObject is IStoreSettingsParent selectedStorage))
                return false;

            _state.CurrentMap = Find.CurrentMap;
            if (_state.CurrentMap == null)
                return false;

            _storageLensUIManager.FetchAndSaveCurrentUIState(_state);

            if (!_state.HasUISnapshotData || !_state.HasStorageTabUIData)
            {
                ClearState();
                return false;
            }

            var currentStorageSettings = selectedStorage.GetStoreSettings();
            if (currentStorageSettings == null)
            {
                ClearState();
                return false;
            }

            _state.SelectedStorage = selectedStorage;
            _state.CurrentStorageSettings = currentStorageSettings;

            if (!_state.IsFullyInitialized)
            {
                ClearState();
                return false;
            }

            return true;
        }

        private bool IsValidStateForUpdate()
        {
            if (!IsActive || !_state.IsFullyInitialized)
                return false;

            if (
                !(
                    Find.Selector.SingleSelectedObject
                    is IStoreSettingsParent currentlySelectedStorage
                )
                || currentlySelectedStorage != _state.SelectedStorage
            )
                return false;

            StorageSettings currentStorageSettings = currentlySelectedStorage.GetStoreSettings();
            if (
                currentStorageSettings == null
                || currentStorageSettings != _state.CurrentStorageSettings
            )
                return false;

            return true;
        }

        private void ClearState()
        {
            _state.ClearAllState();
        }
    }
}
