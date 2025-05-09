using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using PressR.Features.DirectHaul.Core;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    [StaticConstructorOnStartup]
    public class DirectHaulGhostGraphicsController : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulState _state;
        private readonly DirectHaulPreview _preview = new();

        private readonly HashSet<object> _reusableDesiredKeysSet = new HashSet<object>();
        private readonly HashSet<object> _reusableRegisteredKeysSet = new HashSet<object>();

        private const float FadeInDuration = 0.05f;
        private const float FadeOutDuration = 0.05f;

        private const float GhostFillAlpha = 0.1f;
        private const float GhostEdgeSensitivity = 0.25f;

        private static readonly Color PreviewOutlineColor = Color.white;
        private static readonly Color PreviewFillColor = new Color(1f, 1f, 1f, GhostFillAlpha);

        private static readonly Color PendingOutlineColor = Color.white;
        private static readonly Color PendingFillColor = new Color(
            155f / 255f,
            216f / 255f,
            226f / 255f,
            GhostFillAlpha
        );

        private Dictionary<Thing, IntVec3> _lastPreviewPositions = new();

        public DirectHaulGhostGraphicsController(
            IGraphicsManager graphicsManager,
            DirectHaulState state
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Update()
        {
            if (_state.Mode == DirectHaulMode.Storage)
            {
                ClearInternal();
                return;
            }

            if (!PressRMod.Settings.directHaulSettings.enablePlacementGhosts)
            {
                ClearInternal();
                return;
            }

            if (!TryGetContext(_state.Map, out var viewRect, out var directHaulData))
            {
                ClearInternal();
                return;
            }

            _preview.TryGetPreviewPositions(
                _state.StartDragCell.IsValid ? _state.StartDragCell : _state.CurrentMouseCell,
                _state.IsDragging
                    ? _state.CurrentDragCell
                    : (
                        _state.StartDragCell.IsValid
                            ? _state.StartDragCell
                            : _state.CurrentMouseCell
                    ),
                _state,
                out var desiredPreviewPositions
            );

            _lastPreviewPositions = desiredPreviewPositions ?? new Dictionary<Thing, IntVec3>();

            var visiblePreviewPositions = _lastPreviewPositions
                .Where(kvp => kvp.Value.IsValid && viewRect.Contains(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            ProcessGhostUpdates(
                visiblePreviewPositions,
                CreatePreviewGhostKey,
                (thing, cell) =>
                    CreateGhostObject(thing, cell.ToVector3Shifted(), 0f, GhostType.Preview),
                GhostType.Preview
            );

            var visiblePendingTargets = GetVisiblePendingTargets(directHaulData, viewRect);

            var pendingTargetPositions = new Dictionary<Thing, IntVec3>(
                visiblePendingTargets.Count
            );
            foreach (var kvp in visiblePendingTargets)
            {
                pendingTargetPositions.Add(kvp.Key, kvp.Value.Cell);
            }

            ProcessGhostUpdates(
                pendingTargetPositions,
                CreatePendingGhostKey,
                (thing, cell) =>
                    CreateGhostObject(thing, cell.ToVector3Shifted(), 0f, GhostType.Pending),
                GhostType.Pending
            );
        }

        public void Clear()
        {
            ClearInternal();
        }

        private void ClearInternal()
        {
            ClearGraphicObjectsOfGhostType(GhostType.Preview);
            ClearGraphicObjectsOfGhostType(GhostType.Pending);
            _lastPreviewPositions.Clear();
        }

        private bool TryGetContext(Map map, out CellRect viewRect)
        {
            viewRect = default;
            if (_graphicsManager == null || map == null)
                return false;
            viewRect = Find.CameraDriver.CurrentViewRect;
            return true;
        }

        private bool TryGetContext(
            Map map,
            out CellRect viewRect,
            out DirectHaulExposableData directHaulData
        )
        {
            directHaulData = _state?.ExposedData;
            return TryGetContext(map, out viewRect) && directHaulData != null;
        }

        private Dictionary<Thing, LocalTargetInfo> GetVisiblePendingTargets(
            DirectHaulExposableData directHaulData,
            CellRect viewRect
        )
        {
            var heldThingPositions = _state
                .AllHeldThingsOnMap.Where(t => t.PositionHeld.IsValid)
                .Select(t => t.PositionHeld)
                .ToHashSet();

            var allPendingThingsAndTargets = directHaulData.GetPendingThingsAndTargets();

            var filteredPendingData = allPendingThingsAndTargets
                .Where(kvp => !heldThingPositions.Contains(kvp.Value.Cell))
                .ToList();

            var groupedByCell = filteredPendingData.GroupBy(kvp => kvp.Value.Cell);

            var prioritizedPendingTargets = new Dictionary<Thing, LocalTargetInfo>();
            foreach (var group in groupedByCell)
            {
                var cell = group.Key;
                var itemsInGroup = group.ToList();

                if (itemsInGroup.Count == 1)
                {
                    var kvp = itemsInGroup.First();
                    prioritizedPendingTargets[kvp.Key] = kvp.Value;
                }
                else
                {
                    var chosenKvp = itemsInGroup
                        .OrderByDescending(kvp => (kvp.Key.Position - cell).LengthHorizontalSquared)
                        .First();
                    prioritizedPendingTargets[chosenKvp.Key] = chosenKvp.Value;
                }
            }

            return prioritizedPendingTargets
                .Where(kvp => kvp.Value.Cell.IsValid && viewRect.Contains(kvp.Value.Cell))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private void ProcessGhostUpdates(
            Dictionary<Thing, IntVec3> desiredPositions,
            Func<Thing, object> keyFactory,
            Func<Thing, IntVec3, DirectHaulGhostGraphicObject> ghostFactory,
            GhostType currentGhostType
        )
        {
            _reusableDesiredKeysSet.Clear();
            _reusableRegisteredKeysSet.Clear();

            if (desiredPositions.Count == 0)
            {
                ClearGraphicObjectsOfGhostType(currentGhostType);
                return;
            }

            foreach (var kvp in _graphicsManager.GetAllGraphicObjects())
            {
                if (
                    kvp.Key is ValueTuple<Thing, GhostType> tupleKey
                    && tupleKey.Item2 == currentGhostType
                )
                {
                    _reusableRegisteredKeysSet.Add(kvp.Key);
                }
            }

            foreach (var kvpThingCell in desiredPositions)
            {
                Thing thing = kvpThingCell.Key;
                IntVec3 targetCell = kvpThingCell.Value;
                object key = keyFactory(thing);

                _reusableDesiredKeysSet.Add(key);

                if (_reusableRegisteredKeysSet.Contains(key))
                {
                    if (
                        _graphicsManager.TryGetGraphicObject(key, out var existingObject)
                        && existingObject is DirectHaulGhostGraphicObject existingGhost
                    )
                    {
                        if (existingObject.State == GraphicObjectState.PendingRemoval)
                        {
                            _graphicsManager.RegisterGraphicObject(existingObject);
                            ApplyFadeInEffect(existingObject);
                        }
                        existingGhost.Position = targetCell.ToVector3Shifted();
                    }
                }
                else
                {
                    var newGhostObject = ghostFactory(thing, targetCell);
                    if (
                        _graphicsManager.RegisterGraphicObject(newGhostObject)
                            is DirectHaulGhostGraphicObject registeredGhost
                        && registeredGhost != null
                    )
                    {
                        ApplyFadeInEffect(registeredGhost);
                    }
                }
            }

            foreach (var registeredKey in _reusableRegisteredKeysSet)
            {
                if (!_reusableDesiredKeysSet.Contains(registeredKey))
                {
                    RemoveGraphicObjectWithFadeOut(registeredKey);
                }
            }
        }

        private static object CreatePreviewGhostKey(Thing thing) =>
            (object)(thing, GhostType.Preview);

        private static object CreatePendingGhostKey(Thing thing) =>
            (object)(thing, GhostType.Pending);

        private static DirectHaulGhostGraphicObject CreateGhostObject(
            Thing thing,
            Vector3 position,
            float alpha,
            GhostType ghostType
        )
        {
            Color outlineColor =
                ghostType == GhostType.Preview ? PreviewOutlineColor : PendingOutlineColor;
            Color fillColor = ghostType == GhostType.Preview ? PreviewFillColor : PendingFillColor;

            return new DirectHaulGhostGraphicObject(thing, ghostType, position)
            {
                OutlineColor = outlineColor,
                Color = fillColor,
                Alpha = alpha,
                EdgeSensitivity = GhostEdgeSensitivity,
            };
        }

        private void ClearGraphicObjectsOfGhostType(GhostType ghostType)
        {
            if (_graphicsManager == null)
                return;

            List<object> keysToClearList = new List<object>();
            foreach (var kvp in _graphicsManager.GetAllGraphicObjects())
            {
                if (
                    kvp.Key is ValueTuple<Thing, GhostType> tupleKey
                    && tupleKey.Item2 == ghostType
                    && kvp.Value.State == GraphicObjectState.Active
                )
                {
                    keysToClearList.Add(kvp.Key);
                }
            }

            foreach (var key in keysToClearList)
            {
                RemoveGraphicObjectWithFadeOut(key);
            }
        }

        private void ApplyFadeInEffect(IGraphicObject target)
        {
            if (
                !(target is IHasAlpha hasAlpha)
                || target.Key == null
                || target.State != GraphicObjectState.Active
            )
                return;

            _graphicsManager.ApplyTween(
                target.Key,
                () => hasAlpha.Alpha,
                a =>
                {
                    if (target is IHasAlpha ha && target.State == GraphicObjectState.Active)
                        ha.Alpha = a;
                },
                1f,
                FadeInDuration,
                easing: Equations.Linear,
                propertyId: nameof(IHasAlpha.Alpha)
            );
        }

        private void ApplyFadeOutEffect(IGraphicObject target)
        {
            if (!(target is IHasAlpha hasAlpha) || target.Key == null)
                return;

            object key = target.Key;

            if (target.State != GraphicObjectState.Active)
            {
                _graphicsManager.UnregisterGraphicObject(key);
                return;
            }

            _graphicsManager.ApplyTween(
                key,
                () => hasAlpha.Alpha,
                a =>
                {
                    if (target is IHasAlpha ha)
                        ha.Alpha = a;
                },
                0f,
                FadeOutDuration,
                easing: Equations.Linear,
                propertyId: nameof(IHasAlpha.Alpha)
            );
        }

        private void RemoveGraphicObjectWithFadeOut(object key)
        {
            if (_graphicsManager.TryGetGraphicObject(key, out var graphicObject))
            {
                ApplyFadeOutEffect(graphicObject);
                _graphicsManager.UnregisterGraphicObject(key);
            }
        }
    }
}
