using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Graphics.GraphicObjects;
using PressR.Graphics.Effects;
using PressR.Graphics.Interfaces;
using PressR.Graphics.Shaders;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    [StaticConstructorOnStartup]
    public class DirectHaulGhostGraphics
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulFrameData _frameData;

        private const float FadeInDuration = 0.05f;
        private const float FadeOutDuration = 0.05f;

        private const float GhostFillAlpha = 0.1f;
        private const float GhostEdgeSensitivity = 0.25f;

        private static readonly Shader GhostShader = ShaderManager.SobelEdgeDetectShader;

        private static readonly Color PreviewOutlineColor = Color.white;
        private static readonly Color PreviewFillColor = new Color(1f, 1f, 1f, GhostFillAlpha);

        private static readonly Color PendingOutlineColor = Color.white;
        private static readonly Color PendingFillColor = new Color(
            155f / 255f,
            216f / 255f,
            226f / 255f,
            GhostFillAlpha
        );

        public DirectHaulGhostGraphics(
            IGraphicsManager graphicsManager,
            DirectHaulFrameData frameData
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _frameData = frameData ?? throw new ArgumentNullException(nameof(frameData));
        }

        public void UpdatePreviewGhosts(Dictionary<Thing, IntVec3> desiredPreviewPositions, Map map)
        {
            if (!TryGetContext(map, out var viewRect))
            {
                ClearPreviewGhosts();
                return;
            }

            var visiblePreviewPositions = desiredPreviewPositions
                .Where(kvp => kvp.Value.IsValid && viewRect.Contains(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            ProcessGhostUpdates<DirectHaulPreviewGhostGraphicObject>(
                visiblePreviewPositions,
                CreatePreviewGhostKey,
                (thing, cell) =>
                    CreatePreviewGhostObject(thing, cell.ToVector3Shifted(), GhostShader, 0f)
            );
        }

        public void ClearPreviewGhosts()
        {
            ClearGraphicObjectsOfType<DirectHaulPreviewGhostGraphicObject>();
        }

        public void UpdatePendingGhosts(Map map)
        {
            if (!TryGetContext(map, out var viewRect, out var directHaulData))
            {
                ClearPendingGhosts();
                return;
            }

            var visiblePendingTargets = GetVisiblePendingTargets(directHaulData, viewRect);

            ProcessGhostUpdates<DirectHaulPendingGhostGraphicObject>(
                visiblePendingTargets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Cell),
                CreatePendingGhostKey,
                (thing, cell) =>
                    CreatePendingGhostObject(thing, cell.ToVector3Shifted(), GhostShader, 0f)
            );
        }

        public void ClearPendingGhosts()
        {
            ClearGraphicObjectsOfType<DirectHaulPendingGhostGraphicObject>();
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
            directHaulData = _frameData?.ExposedData;
            return TryGetContext(map, out viewRect) && directHaulData != null;
        }

        private Dictionary<Thing, LocalTargetInfo> GetVisiblePendingTargets(
            DirectHaulExposableData directHaulData,
            CellRect viewRect
        )
        {
            var heldThingPositions = _frameData
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

        private void ProcessGhostUpdates<T>(
            Dictionary<Thing, IntVec3> desiredPositions,
            Func<Thing, object> keyFactory,
            Func<Thing, IntVec3, T> ghostFactory
        )
            where T : class, IGraphicObject, IHasPosition
        {
            if (desiredPositions.Count == 0)
            {
                ClearGraphicObjectsOfType<T>();
                return;
            }

            var desiredKeys = desiredPositions.Keys.Select(keyFactory).ToHashSet();
            var registeredKeys = _graphicsManager
                .GetActiveGraphicObjects()
                .Keys.Where(k => k is ValueTuple<Thing, Type> tuple && tuple.Item2 == typeof(T))
                .ToHashSet();

            var keysToRemove = registeredKeys.Except(desiredKeys).ToList();
            var keysToAdd = desiredKeys.Except(registeredKeys).ToHashSet();

            foreach (var keyToRemove in keysToRemove)
            {
                if (
                    _graphicsManager.TryGetGraphicObject(keyToRemove, out var graphicObject)
                    && graphicObject.State == GraphicObjectState.Active
                )
                {
                    ApplyFadeOutEffect(graphicObject);
                    _graphicsManager.UnregisterGraphicObject(keyToRemove);
                }
            }

            foreach (var kvp in desiredPositions)
            {
                Thing thing = kvp.Key;
                IntVec3 targetCell = kvp.Value;
                object key = keyFactory(thing);

                if (
                    _graphicsManager.TryGetGraphicObject(key, out var existingObject)
                    && existingObject is IHasPosition hasPosition
                )
                {
                    if (existingObject.State == GraphicObjectState.PendingRemoval)
                    {
                        _graphicsManager.RegisterGraphicObject(existingObject);
                        ApplyFadeInEffect(existingObject);
                    }
                    hasPosition.Position = targetCell.ToVector3Shifted();
                }
                else if (keysToAdd.Contains(key))
                {
                    var newGhostObject = ghostFactory(thing, targetCell);
                    if (_graphicsManager.RegisterGraphicObject(newGhostObject))
                    {
                        ApplyFadeInEffect(newGhostObject);
                    }
                }
            }
        }

        private static object CreatePreviewGhostKey(Thing thing) =>
            (object)(thing, typeof(DirectHaulPreviewGhostGraphicObject));

        private static object CreatePendingGhostKey(Thing thing) =>
            (object)(thing, typeof(DirectHaulPendingGhostGraphicObject));

        private static DirectHaulPreviewGhostGraphicObject CreatePreviewGhostObject(
            Thing thing,
            Vector3 position,
            Shader shader,
            float alpha
        )
        {
            return new DirectHaulPreviewGhostGraphicObject(thing, position, shader)
            {
                OutlineColor = PreviewOutlineColor,
                Color = PreviewFillColor,
                Alpha = alpha,
                EdgeSensitivity = GhostEdgeSensitivity,
            };
        }

        private static DirectHaulPendingGhostGraphicObject CreatePendingGhostObject(
            Thing thing,
            Vector3 position,
            Shader shader,
            float alpha
        )
        {
            return new DirectHaulPendingGhostGraphicObject(thing, position, shader)
            {
                OutlineColor = PendingOutlineColor,
                Color = PendingFillColor,
                Alpha = alpha,
                EdgeSensitivity = GhostEdgeSensitivity,
            };
        }

        private void ClearGraphicObjectsOfType<T>()
            where T : IGraphicObject
        {
            if (_graphicsManager == null)
                return;

            var keysToClear = _graphicsManager
                .GetActiveGraphicObjects()
                .Where(kvp => kvp.Value is T)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToClear)
            {
                if (
                    _graphicsManager.TryGetGraphicObject(key, out var graphicObject)
                    && graphicObject.State == GraphicObjectState.Active
                )
                {
                    ApplyFadeOutEffect(graphicObject);
                    _graphicsManager.UnregisterGraphicObject(key);
                }
            }
        }

        private void ApplyFadeInEffect(IGraphicObject target)
        {
            StopEffectsOfType<FadeOutEffect>(target.Key);
            var effect = new FadeInEffect(FadeInDuration);
            _graphicsManager.ApplyEffect(new[] { target.Key }, effect);
        }

        private void ApplyFadeOutEffect(IGraphicObject target)
        {
            StopEffectsOfType<FadeInEffect>(target.Key);
            var effect = new FadeOutEffect(FadeOutDuration);
            _graphicsManager.ApplyEffect(new[] { target.Key }, effect);
        }

        private void StopEffectsOfType<T>(object targetKey)
            where T : IEffect =>
            _graphicsManager
                .GetEffectsForTarget(targetKey)
                .OfType<T>()
                .ToList()
                .ForEach(effect => _graphicsManager.StopEffect(effect.Key));
    }
}
