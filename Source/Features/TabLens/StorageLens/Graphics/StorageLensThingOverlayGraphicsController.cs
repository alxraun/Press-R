using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.TabLens.Graphics;
using PressR.Features.TabLens.StorageLens.Core;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;
using PressR.Utils;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Graphics
{
    public class StorageLensThingOverlayGraphicsController : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly TrackedThingsData _trackedThingsData;
        private const float FadeInDuration = 0.05f;
        private const float FadeOutDuration = 0.05f;

        public StorageLensThingOverlayGraphicsController(
            IGraphicsManager graphicsManager,
            TrackedThingsData trackedThingsData
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _trackedThingsData =
                trackedThingsData ?? throw new ArgumentNullException(nameof(trackedThingsData));
        }

        public void Update()
        {
            if (_trackedThingsData?.CurrentThings == null)
            {
                ClearInternal(_graphicsManager);
                return;
            }

            if (!PressRMod.Settings.tabLensSettings.enableStorageLensOverlays)
            {
                ClearInternal(_graphicsManager);
                return;
            }

            HashSet<object> desiredKeys = _trackedThingsData
                .CurrentThings.Select(thing =>
                    (object)(thing, typeof(TabLensThingOverlayGraphicObject))
                )
                .ToHashSet();

            var registeredObjects = _graphicsManager.GetAllGraphicObjects();

            var registeredKeys = registeredObjects
                .Keys.Where(key =>
                    key is ValueTuple<Thing, Type> tuple
                    && tuple.Item2 == typeof(TabLensThingOverlayGraphicObject)
                )
                .ToHashSet();

            var keysToRemove = registeredKeys.Except(desiredKeys).ToList();
            var keysToAdd = desiredKeys.Except(registeredKeys).ToList();
            var keysToUpdate = desiredKeys.Intersect(registeredKeys).ToList();

            RemoveObsoleteOverlays(_graphicsManager, keysToRemove);
            AddNewOverlays(_graphicsManager, keysToAdd, _trackedThingsData);
            UpdateExistingOverlays(
                _graphicsManager,
                keysToUpdate,
                registeredObjects,
                _trackedThingsData
            );
        }

        public void Clear()
        {
            ClearInternal(_graphicsManager);
        }

        private void ClearInternal(IGraphicsManager graphicsManager)
        {
            if (graphicsManager == null)
                return;

            var allObjects = graphicsManager.GetAllGraphicObjects();

            var storageLensKeys = allObjects
                .Where(kvp =>
                    kvp.Key is ValueTuple<Thing, Type> tuple
                    && tuple.Item2 == typeof(TabLensThingOverlayGraphicObject)
                    && kvp.Value.State == GraphicObjectState.Active
                )
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in storageLensKeys)
            {
                RemoveGraphicObjectWithFadeOut(key);
            }
        }

        private void RemoveObsoleteOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToRemove
        )
        {
            keysToRemove.ToList().ForEach(RemoveGraphicObjectWithFadeOut);
        }

        private void AddNewOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToAdd,
            TrackedThingsData trackedThingsData
        )
        {
            keysToAdd
                .ToList()
                .ForEach(key =>
                {
                    if (key is ValueTuple<Thing, Type> { Item1: var thing })
                    {
                        bool allowed = trackedThingsData.GetAllowanceState(thing);
                        Color color = GraphicsUtils.GetColorForState(allowed);

                        var graphicObject = new TabLensThingOverlayGraphicObject(
                            thing,
                            ShaderManager.HSVColorizeCutoutShader
                        );
                        graphicObject.Alpha = 0f;

                        var registeredObject = graphicsManager.RegisterGraphicObject(graphicObject);

                        if (registeredObject is IHasColor colorTarget)
                        {
                            colorTarget.Color = color;
                        }

                        if (registeredObject is IHasAlpha alphaTarget)
                        {
                            ApplyFadeInEffect(graphicsManager, key, alphaTarget);
                        }
                    }
                });
        }

        private void UpdateExistingOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToUpdate,
            IReadOnlyDictionary<object, IGraphicObject> registeredObjects,
            TrackedThingsData trackedThingsData
        )
        {
            keysToUpdate
                .ToList()
                .ForEach(key =>
                {
                    if (key is ValueTuple<Thing, Type> { Item1: var thing })
                    {
                        if (registeredObjects.TryGetValue(key, out IGraphicObject graphicObject))
                        {
                            bool allowed = trackedThingsData.GetAllowanceState(thing);
                            Color desiredColor = GraphicsUtils.GetColorForState(allowed);

                            if (graphicObject is IHasColor colorTarget)
                            {
                                colorTarget.Color = desiredColor;
                            }

                            if (graphicObject.State == GraphicObjectState.PendingRemoval)
                            {
                                var reactivatedObject = graphicsManager.RegisterGraphicObject(
                                    graphicObject
                                );
                                if (
                                    reactivatedObject != null
                                    && reactivatedObject is IHasAlpha alphaTarget
                                )
                                {
                                    ApplyFadeInEffect(graphicsManager, key, alphaTarget);
                                }
                            }
                        }
                    }
                });
        }

        private static void ApplyFadeInEffect(
            IGraphicsManager graphicsManager,
            object key,
            IHasAlpha alphaTarget
        )
        {
            if (alphaTarget == null)
                return;

            graphicsManager.ApplyTween<float>(
                key,
                getter: () => alphaTarget.Alpha,
                setter: value => alphaTarget.Alpha = value,
                endValue: 1.0f,
                duration: FadeInDuration,
                propertyId: nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }

        private static void ApplyFadeOutEffect(
            IGraphicsManager graphicsManager,
            object key,
            IHasAlpha alphaTarget
        )
        {
            if (alphaTarget == null)
                return;

            graphicsManager.ApplyTween<float>(
                key,
                getter: () => alphaTarget.Alpha,
                setter: value => alphaTarget.Alpha = value,
                endValue: 0.0f,
                duration: FadeOutDuration,
                propertyId: nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }

        private void RemoveGraphicObjectWithFadeOut(object key)
        {
            if (_graphicsManager == null)
                return;

            if (
                _graphicsManager.TryGetGraphicObject(key, out var graphicObject)
                && graphicObject is IHasAlpha alphaObj
            )
            {
                ApplyFadeOutEffect(_graphicsManager, key, alphaObj);
            }

            _graphicsManager.UnregisterGraphicObject(key);
        }
    }
}
