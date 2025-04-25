using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Graphics;
using PressR.Graphics.Interfaces;
using PressR.Interfaces;
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
        private readonly DirectHaulFrameData _frameData = new();
        private readonly DirectHaulPlacement _placement = new();
        private readonly DirectHaulInput _input = new();
        private readonly DirectHaulPreview _preview = new();
        private readonly DirectHaulDragState _dragState = new();
        private readonly DirectHaulSoundPlayer _soundPlayer = new();
        private readonly DirectHaulStorage _directHaulStorage;
        private readonly DirectHaulThingState _thingState;

        private readonly DirectHaulStatusOverlayGraphics _statusOverlayGraphics;
        private readonly DirectHaulGhostGraphics _ghostGraphics;
        private readonly DirectHaulRadiusIndicatorGraphics _radiusIndicatorGraphics;
        private readonly DirectHaulStorageRectGraphics _storageRectGraphics;
        private readonly DirectHaulStorageHighlightGraphics _storageHighlightGraphics;

        private bool IsFeatureEnabled => PressRMod.Settings.enableDirectHaul;
        private DirectHaulSettings DirectHaulSettings => PressRMod.Settings.directHaulSettings;

        public DirectHaulFeature(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));

            _thingState = new DirectHaulThingState(_frameData);
            _directHaulStorage = new DirectHaulStorage();

            _statusOverlayGraphics = new DirectHaulStatusOverlayGraphics(_graphicsManager);
            _ghostGraphics = new DirectHaulGhostGraphics(_graphicsManager, _frameData);
            _radiusIndicatorGraphics = new DirectHaulRadiusIndicatorGraphics(
                _graphicsManager,
                _frameData
            );
            _storageRectGraphics = new DirectHaulStorageRectGraphics(
                _graphicsManager,
                _directHaulStorage
            );
            _storageHighlightGraphics = new DirectHaulStorageHighlightGraphics(_graphicsManager);
        }

        public void ConstantUpdate()
        {
            if (Find.CurrentMap is Map map)
            {
                if (DirectHaulSettings.enableStatusOverlays)
                {
                    _statusOverlayGraphics.UpdateDirectHaulStatusOverlays(map);
                }
                else
                {
                    _statusOverlayGraphics.ClearAllOverlays();
                }
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

            _dragState.Reset();
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
            _dragState.Reset();
            ClearAllVisuals();
            _soundPlayer.EndDragSustainer();
        }

        public void Update()
        {
            if (!IsActive || !IsFeatureEnabled)
            {
                if (!IsFeatureEnabled && IsActive)
                    Deactivate();
                return;
            }

            if (!TryGetValidHaulableSelectionContext(out Map map, out List<Thing> selectedThings))
            {
                Deactivate();
                return;
            }

            CurrentFrameState state = CalculateCurrentFrameState(map, selectedThings);

            if (state.IsValidContext)
            {
                EnsurePlacementCellsAreCalculated(state);
            }

            _soundPlayer.UpdateDragSound(
                _dragState,
                state.Mode,
                _frameData.CalculatedPlacementCells,
                state.Map
            );

            ProcessInput(map, state);
            UpdateVisuals(map, state);
        }

        private bool TryGetValidHaulableSelectionContext(
            out Map map,
            out List<Thing> selectedThings
        )
        {
            map = Find.CurrentMap;
            selectedThings = [];

            if (map is null)
            {
                return false;
            }

            selectedThings = SelectionUtils.GetSelectedHaulableThings().ToList();
            return selectedThings.Any();
        }

        private CurrentFrameState CalculateCurrentFrameState(Map map, List<Thing> selectedThings)
        {
            _frameData.Update(map, selectedThings);

            IntVec3 currentMouseCell = _input.GetMouseCell();
            IntVec3 focus1 = _dragState.StartDragCell.IsValid
                ? _dragState.StartDragCell
                : currentMouseCell;
            IntVec3 focus2 = _dragState.IsDragging ? _dragState.CurrentDragCell : focus1;
            DirectHaulMode mode = _input.GetDirectHaulMode();

            bool isValidContext = HasValidActionContext(focus1, focus2, map);

            return new CurrentFrameState(
                map,
                focus1,
                focus2,
                mode,
                isValidContext,
                currentMouseCell
            );
        }

        private bool HasValidActionContext(IntVec3 focus1, IntVec3 focus2, Map map) =>
            map is not null
            && _frameData.HasAnyNonPendingSelected
            && focus1.IsValid
            && focus1.InBounds(map)
            && !focus1.Impassable(map)
            && focus2.IsValid
            && focus2.InBounds(map)
            && !focus2.Impassable(map);

        private void EnsurePlacementCellsAreCalculated(CurrentFrameState state)
        {
            if (
                !_frameData.HasValidCalculatedPlacementCells(state.Focus1, state.Focus2, state.Mode)
            )
            {
                CalculateAndCachePlacementCells(state.Focus1, state.Focus2, state.Mode, state.Map);
            }
        }

        private void CalculateAndCachePlacementCells(
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulMode mode,
            Map map
        ) =>
            _frameData.SetCalculatedPlacementCells(
                _placement.FindPlacementCells(focus1, focus2, _frameData, map),
                focus1,
                focus2,
                mode
            );

        private void ProcessInput(Map map, CurrentFrameState state)
        {
            if (_input.IsTriggerDown())
            {
                HandleTriggerDown(state);
            }
            else if (_input.IsTriggerHeld())
            {
                HandleTriggerHeld(state);
            }
            else if (_input.IsTriggerUp())
            {
                HandleTriggerUp(map, state);
            }
        }

        private void HandleTriggerDown(CurrentFrameState state)
        {
            _input.TryUseEvent();
            if (state.IsValidContext)
            {
                _dragState.StartDrag(state.CurrentMouseCell);
            }
        }

        private void HandleTriggerHeld(CurrentFrameState state)
        {
            if (_dragState.StartDragCell.IsValid)
            {
                _dragState.UpdateDrag(state.CurrentMouseCell);
            }
        }

        private void HandleTriggerUp(Map map, CurrentFrameState state)
        {
            _dragState.EndDrag();
            if (_dragState.IsCompleted)
            {
                TryExecuteAction(map, state);
            }
        }

        private void TryExecuteAction(Map map, CurrentFrameState state)
        {
            IntVec3 startFocus = _dragState.StartDragCell;
            IntVec3 endFocus = _dragState.CurrentDragCell.IsValid
                ? _dragState.CurrentDragCell
                : state.Focus1;

            if (!HasValidActionContext(startFocus, endFocus, map))
            {
                _dragState.Reset();
                return;
            }

            ExecuteActionForMode(map, startFocus, endFocus, state.Mode);
            _dragState.Reset();
        }

        private void ExecuteActionForMode(
            Map map,
            IntVec3 startFocus,
            IntVec3 endFocus,
            DirectHaulMode mode
        )
        {
            List<Thing> affectedThings = PerformActionInternal(
                mode,
                startFocus,
                endFocus,
                map,
                _frameData
            );

            if (mode != DirectHaulMode.Storage && affectedThings.Any())
            {
                _frameData.UpdatePendingStatusLocally(affectedThings);
                SoundDefOf.Designate_ZoneAdd_Dumping.PlayOneShotOnCamera();
            }
        }

        private List<Thing> PerformActionInternal(
            DirectHaulMode mode,
            IntVec3 focus1,
            IntVec3 focus2,
            Map map,
            DirectHaulFrameData frameData
        )
        {
            if (frameData is null || map is null)
            {
                return [];
            }

            if (mode != DirectHaulMode.Storage && !frameData.HasAnyNonPendingSelected)
            {
                return [];
            }

            var thingsToPlace = frameData.NonPendingSelectedThings;
            var placementCells = frameData.CalculatedPlacementCells;

            if (
                mode != DirectHaulMode.Storage
                && (placementCells is null || placementCells.Count < thingsToPlace.Count)
            )
            {
                return [];
            }

            switch (mode)
            {
                case DirectHaulMode.Standard:
                    return ExecuteStandardAction(placementCells, isHighPriority: false);
                case DirectHaulMode.HighPriority:
                    return ExecuteStandardAction(placementCells, isHighPriority: true);
                case DirectHaulMode.Storage:
                    ExecuteStorageAction(focus1, focus2, thingsToPlace);
                    return [];
                default:
                    return [];
            }
        }

        private List<Thing> ExecuteStandardAction(
            IReadOnlyList<IntVec3> placementCells,
            bool isHighPriority
        ) => _thingState.MarkThingsAsPending(placementCells, isHighPriority);

        private List<Thing> ExecuteStorageAction(
            IntVec3 startCell,
            IntVec3 endCell,
            IReadOnlyList<Thing> thingsToStore
        )
        {
            bool wasDrag = startCell != endCell;

            if (wasDrag)
            {
                _directHaulStorage.GetOrCreateStockpileForAction(startCell, endCell);
            }
            else
            {
                var targetStorage = _directHaulStorage.FindStorageAt(endCell);
                if (targetStorage != null)
                {
                    var defsToAllow = thingsToStore.Select(t => t.def).Distinct();
                    _directHaulStorage.ToggleThingDefsAllowance(targetStorage, defsToAllow);
                }
            }

            _thingState.RemoveNonPendingSelectedThingsFromTracking();
            return [];
        }

        private void UpdateVisuals(Map map, CurrentFrameState state)
        {
            if (!state.Focus1.IsValid || !state.Focus2.IsValid)
            {
                ClearAllVisuals();
                return;
            }

            var storageUnderMouse = MapUtils.GetThingOrZoneAtMouseCell<IStoreSettingsParent>(map);

            switch (state.Mode)
            {
                case DirectHaulMode.Storage:
                    RenderStorageVisuals(map, state, storageUnderMouse);
                    break;
                default:
                    RenderStandardVisuals(map, state);
                    break;
            }
        }

        private void RenderStandardVisuals(Map map, CurrentFrameState state)
        {
            ClearStorageVisuals();
            _preview.TryGetPreviewPositions(
                state.Focus1,
                state.Focus2,
                map,
                _frameData,
                out var previewPositions
            );

            if (DirectHaulSettings.enablePlacementGhosts)
            {
                if (previewPositions?.Count > 0)
                {
                    _ghostGraphics.UpdatePreviewGhosts(previewPositions, map);
                }
                else
                {
                    _ghostGraphics.ClearPreviewGhosts();
                }
                _ghostGraphics.UpdatePendingGhosts(map);
            }

            if (DirectHaulSettings.enableRadiusIndicator)
            {
                _radiusIndicatorGraphics.UpdateRadiusIndicator(
                    state.CurrentMouseCell,
                    previewPositions,
                    _dragState.IsDragging,
                    map
                );
            }
        }

        private void RenderStorageVisuals(
            Map map,
            CurrentFrameState state,
            IStoreSettingsParent storageUnderMouse
        )
        {
            ClearStandardVisuals();

            if (DirectHaulSettings.enableStorageCreationPreview)
            {
                _storageRectGraphics.UpdateStorageRect(
                    _dragState.IsDragging,
                    _dragState.StartDragCell,
                    _dragState.CurrentDragCell,
                    map
                );
            }

            if (DirectHaulSettings.enableStorageHighlightOnHover)
            {
                if (!_dragState.IsDragging && storageUnderMouse != null)
                {
                    _storageHighlightGraphics.UpdateHighlight(storageUnderMouse, map, _frameData);
                }
                else
                {
                    _storageHighlightGraphics.ClearHighlight();
                }
            }
        }

        private void ClearAllVisuals()
        {
            ClearStandardVisuals();
            ClearStorageVisuals();
        }

        private void ClearStandardVisuals()
        {
            _ghostGraphics.ClearPreviewGhosts();
            _ghostGraphics.ClearPendingGhosts();
            _radiusIndicatorGraphics.ClearRadiusIndicator();
        }

        private void ClearStorageVisuals()
        {
            _storageRectGraphics.Clear();
            _storageHighlightGraphics.ClearHighlight();
        }

        private readonly struct CurrentFrameState
        {
            public readonly Map Map;
            public readonly IntVec3 Focus1;
            public readonly IntVec3 Focus2;
            public readonly DirectHaulMode Mode;
            public readonly bool IsValidContext;
            public readonly IntVec3 CurrentMouseCell;

            public CurrentFrameState(
                Map map,
                IntVec3 focus1,
                IntVec3 focus2,
                DirectHaulMode mode,
                bool isValidContext,
                IntVec3 currentMouseCell
            )
            {
                Map = map;
                Focus1 = focus1;
                Focus2 = focus2;
                Mode = mode;
                IsValidContext = isValidContext;
                CurrentMouseCell = currentMouseCell;
            }
        }
    }
}
