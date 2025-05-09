using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.TabLens.Graphics;
using PressR.Features.TabLens.StorageLens.Commands;
using PressR.Features.TabLens.StorageLens.Graphics;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Utils;
using PressR.Utils.Throttler;
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
        private readonly StorageLensThingOverlayGraphicsController _thingOverlayController;
        private readonly StorageLensState _state;
        private readonly StorageLensThingsProvider _thingsProvider;
        private readonly StorageLensUIManager _storageLensUIManager;
        private readonly StorageLensInputHandler _inputHandler;
        private readonly List<IGraphicsController> _lensActiveControllers;
        private readonly Throttler _graphicsControllersUpdateThrottler;

        public bool IsActive { get; private set; }

        private const int GraphicsControllersUpdateIntervalTicks = 1;

        public StorageLens(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _state = new StorageLensState();
            _thingOverlayController = new StorageLensThingOverlayGraphicsController(
                _graphicsManager,
                _state
            );
            _thingsProvider = new StorageLensThingsProvider();
            _storageLensUIManager = new StorageLensUIManager();
            _inputHandler = new StorageLensInputHandler(_state);
            _graphicsControllersUpdateThrottler = new Throttler(
                GraphicsControllersUpdateIntervalTicks
            );

            _lensActiveControllers = new List<IGraphicsController> { _thingOverlayController };
            _lensActiveControllers.RemoveAll(item => item == null);
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
            foreach (var controller in _lensActiveControllers)
            {
                controller.Clear();
            }
            ClearState();
            IsActive = false;
        }

        public void Update()
        {
            if (!IsValidStateForUpdate())
            {
                IsActive = false;
                foreach (var controller in _lensActiveControllers)
                {
                    controller.Clear();
                }
                return;
            }

            UpdateTrackedThings();

            if (PressRMod.Settings.tabLensSettings.FocusItemInTabOnHover)
            {
                _inputHandler.ProcessHoverEvent();
            }

            if (_graphicsControllersUpdateThrottler.ShouldExecute())
            {
                foreach (var controller in _lensActiveControllers)
                {
                    controller.Update();
                }
            }

            if (PressRInput.IsMouseButtonDown)
            {
                _inputHandler.ProcessClickEvent();
                Event.current.Use();
            }
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

            _thingsProvider.UpdateVisibleAllowedByParentHaulableThingsInSet(
                _state.CurrentThings,
                _state.CurrentMap,
                _state.SelectedStorage
            );

            UpdateItemAllowanceStates(_state.CurrentThings, _state.CurrentStorageSettings);
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

        private void UpdateItemAllowanceStates(
            IEnumerable<Thing> things,
            StorageSettings currentSettings
        )
        {
            _state.AllowanceStates.Clear();
            ThingFilter currentFilter = currentSettings?.filter;

            if (currentFilter == null)
            {
                foreach (var thing in things)
                {
                    if (thing != null)
                        _state.AllowanceStates[thing] = false;
                }
                return;
            }

            foreach (var thing in things)
            {
                if (thing?.def != null)
                {
                    _state.AllowanceStates[thing] = currentFilter.Allows(thing.def);
                }
            }
        }

        private void ClearState()
        {
            _state.ClearAllState();
        }
    }
}
