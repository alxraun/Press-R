using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.TabLens.Graphics;
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
        private readonly StorageLensState _state;
        private const float FadeInDuration = 0.05f;
        private const float FadeOutDuration = 0.05f;
        private const float SubsequentFadeInDuration = 0.2f;
        private bool _isInitialActivation = true;

        private static readonly Type _graphicObjectType = typeof(TabLensThingOverlayGraphicObject);

        private readonly HashSet<object> _desiredKeysSet = new HashSet<object>();
        private readonly HashSet<object> _registeredKeysSet = new HashSet<object>();
        private readonly List<object> _keysToAddList = new List<object>();
        private readonly List<object> _keysToUpdateList = new List<object>();
        private readonly List<object> _keysToReactivateList = new List<object>();
        private readonly List<object> _keysToRemoveList = new List<object>();

        public StorageLensThingOverlayGraphicsController(
            IGraphicsManager graphicsManager,
            StorageLensState state
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Update()
        {
            if (
                _state?.CurrentThings == null
                || !PressRMod.Settings.tabLensSettings.enableStorageLensOverlays
            )
            {
                ClearInternal(_graphicsManager);
                return;
            }

            _desiredKeysSet.Clear();
            foreach (var thing in _state.CurrentThings)
            {
                _desiredKeysSet.Add((object)(thing, _graphicObjectType));
            }

            var registeredObjects = _graphicsManager.GetAllGraphicObjects();
            _registeredKeysSet.Clear();
            foreach (var key in registeredObjects.Keys)
            {
                if (key is ValueTuple<Thing, Type> tuple && tuple.Item2 == _graphicObjectType)
                {
                    _registeredKeysSet.Add(key);
                }
            }

            _keysToRemoveList.Clear();
            foreach (var registeredKey in _registeredKeysSet)
            {
                if (!_desiredKeysSet.Contains(registeredKey))
                {
                    _keysToRemoveList.Add(registeredKey);
                }
            }

            _keysToAddList.Clear();
            _keysToUpdateList.Clear();
            foreach (var key in _desiredKeysSet)
            {
                if (_registeredKeysSet.Contains(key))
                {
                    _keysToUpdateList.Add(key);
                }
                else
                {
                    _keysToAddList.Add(key);
                }
            }

            _keysToReactivateList.Clear();
            foreach (var k in _keysToUpdateList)
            {
                if (
                    registeredObjects.TryGetValue(k, out var obj)
                    && obj.State == GraphicObjectState.PendingRemoval
                )
                {
                    _keysToReactivateList.Add(k);
                }
            }

            RemoveObsoleteOverlays(_graphicsManager, _keysToRemoveList);

            float currentFadeInDuration = _isInitialActivation
                ? FadeInDuration
                : SubsequentFadeInDuration;

            AddNewOverlays(_graphicsManager, _keysToAddList, _state, currentFadeInDuration);
            UpdateExistingOverlays(
                _graphicsManager,
                _keysToUpdateList,
                registeredObjects,
                _state,
                currentFadeInDuration
            );

            if (_isInitialActivation && (_keysToAddList.Any() || _keysToReactivateList.Any()))
            {
                _isInitialActivation = false;
            }
        }

        public void Clear()
        {
            ClearInternal(_graphicsManager);
        }

        private void ClearInternal(IGraphicsManager graphicsManager)
        {
            if (graphicsManager == null)
                return;

            _isInitialActivation = true;

            var allObjects = graphicsManager.GetAllGraphicObjects();

            foreach (var kvp in allObjects)
            {
                if (
                    kvp.Key is ValueTuple<Thing, Type> tuple
                    && tuple.Item2 == _graphicObjectType
                    && kvp.Value.State == GraphicObjectState.Active
                )
                {
                    RemoveGraphicObjectWithFadeOut(kvp.Key);
                }
            }
        }

        private void RemoveObsoleteOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToRemove
        )
        {
            foreach (var key in keysToRemove)
            {
                RemoveGraphicObjectWithFadeOut(key);
            }
        }

        private void AddNewOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToAdd,
            StorageLensState state,
            float fadeInDuration
        )
        {
            foreach (var key in keysToAdd)
            {
                if (key is ValueTuple<Thing, Type> { Item1: var thing })
                {
                    bool allowed = state.GetAllowanceState(thing);
                    Color color = GraphicsUtils.GetColorForState(allowed);

                    var graphicObject = new TabLensThingOverlayGraphicObject(thing);
                    graphicObject.Alpha = 0f;

                    var registeredObject = graphicsManager.RegisterGraphicObject(graphicObject);

                    if (registeredObject is IHasColor colorTarget)
                    {
                        colorTarget.Color = color;
                    }

                    if (registeredObject is IHasAlpha alphaTarget)
                    {
                        ApplyFadeInEffect(graphicsManager, key, alphaTarget, fadeInDuration);
                    }
                }
            }
        }

        private void UpdateExistingOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToUpdate,
            IReadOnlyDictionary<object, IGraphicObject> registeredObjects,
            StorageLensState state,
            float fadeInDuration
        )
        {
            foreach (var key in keysToUpdate)
            {
                if (key is ValueTuple<Thing, Type> { Item1: var thing })
                {
                    if (registeredObjects.TryGetValue(key, out IGraphicObject graphicObject))
                    {
                        bool allowed = state.GetAllowanceState(thing);
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
                                ApplyFadeInEffect(
                                    graphicsManager,
                                    key,
                                    alphaTarget,
                                    fadeInDuration
                                );
                            }
                        }
                    }
                }
            }
        }

        private static void ApplyFadeInEffect(
            IGraphicsManager graphicsManager,
            object key,
            IHasAlpha alphaTarget,
            float duration
        )
        {
            if (alphaTarget == null)
                return;

            graphicsManager.ApplyTween<float>(
                key,
                getter: () => alphaTarget.Alpha,
                setter: value => alphaTarget.Alpha = value,
                endValue: 1.0f,
                duration: duration,
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
