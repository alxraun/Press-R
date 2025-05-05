using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Graphics;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Settings;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul
{
    public class DirectHaulFeature : IPressRFeature
    {
        public string FeatureId => "DirectHaul";
        public string Label => "Direct Haul";
        public bool IsActive { get; private set; }

        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulState _state;
        private readonly DirectHaulPlacement _placement = new();
        private readonly DirectHaulInput _input = new();
        private readonly DirectHaulSoundPlayer _soundPlayer;
        private readonly DirectHaulStorage _directHaulStorage;

        private readonly DirectHaulStatusOverlayGraphicsController _statusOverlayController;
        private readonly DirectHaulGhostGraphicsController _ghostController;
        private readonly DirectHaulRadiusIndicatorGraphicsController _radiusIndicatorController;
        private readonly DirectHaulStorageRectGraphicsController _storageRectController;
        private readonly DirectHaulStorageHighlightGraphicsController _storageHighlightController;

        private readonly List<IGraphicsController> _featureActiveControllers;

        private bool IsFeatureEnabled => PressRMod.Settings.enableDirectHaul;
        private DirectHaulSettings DirectHaulSettings => PressRMod.Settings.directHaulSettings;

        public DirectHaulFeature(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));

            _state = new DirectHaulState();
            _directHaulStorage = new DirectHaulStorage();
            _soundPlayer = new DirectHaulSoundPlayer(_state);

            _statusOverlayController = new DirectHaulStatusOverlayGraphicsController(
                _graphicsManager
            );
            _ghostController = new DirectHaulGhostGraphicsController(_graphicsManager, _state);
            _radiusIndicatorController = new DirectHaulRadiusIndicatorGraphicsController(
                _graphicsManager,
                _state
            );
            _storageRectController = new DirectHaulStorageRectGraphicsController(
                _graphicsManager,
                _directHaulStorage,
                _state
            );
            _storageHighlightController = new DirectHaulStorageHighlightGraphicsController(
                _graphicsManager,
                _state
            );

            _featureActiveControllers = new List<IGraphicsController>
            {
                _ghostController,
                _radiusIndicatorController,
                _storageRectController,
                _storageHighlightController,
            };
            _featureActiveControllers.RemoveAll(item => item == null);
        }

        public void ConstantUpdate()
        {
            if (Find.CurrentMap is Map map)
            {
                bool enableOverlays = DirectHaulSettings.enableStatusOverlays;
                _statusOverlayController.ConstantUpdate(map, enableOverlays);
            }
            else
            {
                _statusOverlayController.Clear();
            }
        }

        public bool TryActivate()
        {
            if (!IsFeatureEnabled)
                return false;

            if (!SelectionUtils.HasSelectedHaulableThings())
            {
                return false;
            }

            _state.Clear();
            IsActive = true;
            return true;
        }

        public void Deactivate()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _state.Clear();
            ClearAllVisuals();
            _soundPlayer.EndDragSustainer();
        }

        public void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (Find.CurrentMap is not Map map)
            {
                Deactivate();
                return;
            }
            var selectedThings = SelectionUtils.GetSelectedHaulableThings().ToList();
            if (!selectedThings.Any())
            {
                Deactivate();
                return;
            }
            _state.UpdateSelectionAndTrackedData(map, selectedThings);
            IntVec3 currentMouseCell = _input.GetMouseCell();
            _state.SetCurrentMouseCell(currentMouseCell);
            DirectHaulMode mode = _input.GetDirectHaulMode();
            _state.SetCurrentMode(mode);
            _state.SetStorageUnderMouse(_directHaulStorage.FindStorageAt(currentMouseCell));

            ProcessDragInput(currentMouseCell);

            IntVec3 focus1 = _state.StartDragCell.IsValid ? _state.StartDragCell : currentMouseCell;
            IntVec3 focus2 = _state.IsDragging ? _state.CurrentDragCell : focus1;
            if (mode != DirectHaulMode.Storage && IsValidPlacementContext(focus1, focus2, map))
            {
                if (!_state.HasValidCalculatedPlacementCells(focus1, focus2, mode))
                {
                    CalculateAndCachePlacementCells(focus1, focus2, mode);
                }
            }

            _soundPlayer.UpdateSound();

            ProcessActionInput(map, mode, focus1, focus2);

            _statusOverlayController.Update();

            foreach (var controller in _featureActiveControllers)
            {
                controller.Update();
            }
        }

        private bool IsValidPlacementContext(IntVec3 focus1, IntVec3 focus2, Map map) =>
            map is not null
            && _state.HasAnyNonPendingSelected
            && focus1.IsValid
            && focus1.InBounds(map)
            && !focus1.Impassable(map)
            && focus2.IsValid
            && focus2.InBounds(map)
            && !focus2.Impassable(map);

        private void CalculateAndCachePlacementCells(
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulMode mode
        )
        {
            var placementCells = _placement.FindPlacementCells(_state);
            _state.SetCalculatedPlacementCells(placementCells, focus1, focus2, mode);
        }

        private void ProcessDragInput(IntVec3 currentMouseCell)
        {
            if (_input.IsTriggerDown())
            {
                IntVec3 focus1 = _state.StartDragCell.IsValid
                    ? _state.StartDragCell
                    : currentMouseCell;
                IntVec3 focus2 = _state.IsDragging ? _state.CurrentDragCell : focus1;
                if (
                    IsValidPlacementContext(focus1, focus2, _state.Map)
                    || _state.Mode == DirectHaulMode.Storage
                )
                {
                    if (_input.TryUseEvent())
                    {
                        _state.StartDrag(currentMouseCell);
                    }
                }
            }
            else if (_input.IsTriggerHeld())
            {
                if (_state.StartDragCell.IsValid)
                {
                    _state.UpdateDrag(currentMouseCell);
                }
            }
        }

        private void ProcessActionInput(
            Map map,
            DirectHaulMode mode,
            IntVec3 focus1,
            IntVec3 focus2
        )
        {
            if (_input.IsTriggerUp())
            {
                bool wasDragging = _state.IsDragging;
                bool startedDrag = _state.StartDragCell.IsValid;

                _state.EndDrag();

                if (startedDrag)
                {
                    if (_input.TryUseEvent())
                    {
                        ExecuteActionForMode(map, focus1, focus2, mode, wasDragging);
                    }
                }
                _state.ResetDragState();
            }
        }

        private void ExecuteActionForMode(
            Map map,
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulMode mode,
            bool wasDragging
        )
        {
            if (mode == DirectHaulMode.Storage)
            {
                if (wasDragging)
                {
                    DirectHaulActions.ExecuteStockpileZoneCreationOrExpansion(
                        _state,
                        _directHaulStorage,
                        focus1,
                        focus2
                    );
                }
                else
                {
                    IStoreSettingsParent storageUnderClick = _directHaulStorage.FindStorageAt(
                        focus1
                    );
                    DirectHaulActions.ExecuteStorageClickAction(
                        _state,
                        _directHaulStorage,
                        storageUnderClick
                    );
                }
                return;
            }

            if (!IsValidPlacementContext(focus1, focus2, map))
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            if (!wasDragging)
            {
                if (_state.HasAnyNonPendingSelected)
                {
                    DirectHaulActions.ExecutePlacementDesignation(
                        _state,
                        _placement,
                        focus1,
                        focus2,
                        mode
                    );
                }
                else if (_state.HasAnyPendingSelected || _state.HasAnyHeldSelected)
                {
                    DirectHaulActions.ExecuteCancelPlacementDesignation(_state);
                }
            }
            else
            {
                DirectHaulActions.ExecutePlacementDesignation(
                    _state,
                    _placement,
                    focus1,
                    focus2,
                    mode
                );
            }
        }

        private void ClearAllVisuals()
        {
            foreach (var controller in _featureActiveControllers)
            {
                controller.Clear();
            }
            _statusOverlayController.Clear();
        }
    }
}
