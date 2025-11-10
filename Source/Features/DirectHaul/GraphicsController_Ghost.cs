using System;
using System.Collections.Generic;
using System.Linq;
using Microtools.Graphics;
using Microtools.Graphics.Tween;
using UnityEngine;
using Verse;

namespace Microtools.Features.DirectHaul
{
    [StaticConstructorOnStartup]
    public sealed class GraphicsController_Ghost(
        IGraphicsManager graphicsManager,
        State state,
        ThingStateManager thingStateManager
    )
    {
        private readonly IGraphicsManager _graphicsManager = graphicsManager;
        private readonly State _state = state;
        private readonly ThingStateManager _thingStateManager = thingStateManager;

        private readonly HashSet<object> _reusableDesiredKeysSet = [];
        private readonly HashSet<object> _reusableRegisteredKeysSet = [];

        private const float FadeInDuration = 0.05f;
        private const float FadeOutDuration = 0.05f;

        private const float GhostFillAlpha = 0.1f;
        private const float GhostEdgeSensitivity = 0.25f;

        private static readonly Color PreviewOutlineColor = Color.white;
        private static readonly Color PreviewFillColor = new(1f, 1f, 1f, GhostFillAlpha);

        private static readonly Color PendingOutlineColor = Color.white;
        private static readonly Color PendingFillColor = new(
            155f / 255f,
            216f / 255f,
            226f / 255f,
            GhostFillAlpha
        );

        public void Update()
        {
            if (!MicrotoolsMod.Settings.directHaulSettings.enablePlacementGhosts)
            {
                Clear();
                return;
            }

            if (!TryGetContext(out var viewRect))
            {
                Clear();
                return;
            }

            var visiblePreviewPositions = _state
                .GhostPlacements.Where(kvp => kvp.Value.IsValid && viewRect.Contains(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            ProcessGhostUpdates(
                visiblePreviewPositions,
                CreatePreviewGhostKey,
                (thing, cell) =>
                    CreateGhostObject(thing, cell.ToVector3Shifted(), 0f, GhostType.Preview),
                GhostType.Preview
            );

            var visiblePendingTargets = GetVisiblePendingTargets(viewRect);

            ProcessGhostUpdates(
                visiblePendingTargets,
                CreatePendingGhostKey,
                (thing, cell) =>
                    CreateGhostObject(thing, cell.ToVector3Shifted(), 0f, GhostType.Pending),
                GhostType.Pending
            );
        }

        public void Clear()
        {
            ClearGraphicObjectsOfGhostType(GhostType.Preview);
            ClearGraphicObjectsOfGhostType(GhostType.Pending);
        }

        private bool TryGetContext(out CellRect viewRect)
        {
            viewRect = default;
            if (_graphicsManager == null || _state.CurrentMap == null)
            {
                return false;
            }
            viewRect = Find.CameraDriver.CurrentViewRect;
            return true;
        }

        private Dictionary<Thing, IntVec3> GetVisiblePendingTargets(CellRect viewRect)
        {
            var visiblePendingThings = new Dictionary<IntVec3, List<Thing>>();

            foreach (var thing in _thingStateManager.AllPendingThings)
            {
                if (
                    _state.TrackedThings.TryGetValue(thing, out var info)
                    && info.Status == DirectHaulStatus.Pending
                )
                {
                    var cell = info.TargetCell.Cell;
                    if (cell.IsValid && viewRect.Contains(cell))
                    {
                        if (!visiblePendingThings.ContainsKey(cell))
                        {
                            visiblePendingThings[cell] = [];
                        }
                        visiblePendingThings[cell].Add(thing);
                    }
                }
            }

            var prioritizedTargets = new Dictionary<Thing, IntVec3>();
            foreach (var group in visiblePendingThings)
            {
                var cell = group.Key;
                var itemsInGroup = group.Value;

                if (itemsInGroup.Count == 1)
                {
                    prioritizedTargets[itemsInGroup.First()] = cell;
                }
                else
                {
                    var chosenThing = itemsInGroup
                        .OrderByDescending(t => (t.Position - cell).LengthHorizontalSquared)
                        .First();
                    prioritizedTargets[chosenThing] = cell;
                }
            }

            return prioritizedTargets;
        }

        private void ProcessGhostUpdates(
            Dictionary<Thing, IntVec3> desiredPositions,
            Func<Thing, object> keyFactory,
            Func<Thing, IntVec3, GraphicObject_Ghost> ghostFactory,
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
                var thing = kvpThingCell.Key;
                var targetCell = kvpThingCell.Value;
                var key = keyFactory(thing);

                _reusableDesiredKeysSet.Add(key);

                if (_reusableRegisteredKeysSet.Contains(key))
                {
                    if (
                        _graphicsManager.TryGetGraphicObject(key, out var existingObject)
                        && existingObject is GraphicObject_Ghost existingGhost
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
                        is GraphicObject_Ghost registeredGhost
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

        private static object CreatePreviewGhostKey(Thing thing) => (thing, GhostType.Preview);

        private static object CreatePendingGhostKey(Thing thing) => (thing, GhostType.Pending);

        private static GraphicObject_Ghost CreateGhostObject(
            Thing thing,
            Vector3 position,
            float alpha,
            GhostType ghostType
        )
        {
            var outlineColor =
                ghostType == GhostType.Preview ? PreviewOutlineColor : PendingOutlineColor;
            var fillColor = ghostType == GhostType.Preview ? PreviewFillColor : PendingFillColor;

            return new GraphicObject_Ghost(thing, ghostType, position)
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
            {
                return;
            }

            var keysToClearList = new List<object>();
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
                target is not IHasAlpha hasAlpha
                || target.Key == null
                || target.State != GraphicObjectState.Active
            )
            {
                return;
            }

            _graphicsManager.ApplyTween(
                target.Key,
                () => hasAlpha.Alpha,
                a =>
                {
                    if (target is IHasAlpha ha && target.State == GraphicObjectState.Active)
                    {
                        ha.Alpha = a;
                    }
                },
                1f,
                FadeInDuration,
                nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }

        private void ApplyFadeOutEffect(IGraphicObject target)
        {
            if (target is not IHasAlpha hasAlpha || target.Key == null)
            {
                return;
            }

            var key = target.Key;

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
                    {
                        ha.Alpha = a;
                    }
                },
                0f,
                FadeOutDuration,
                nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
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
