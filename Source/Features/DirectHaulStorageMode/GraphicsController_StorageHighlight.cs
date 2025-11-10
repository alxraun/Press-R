using System;
using System.Collections.Generic;
using System.Linq;
using Microtools.Graphics;
using Microtools.Graphics.Tween;
using RimWorld;
using UnityEngine;
using Verse;

namespace Microtools.Features.DirectHaulStorageMode
{
    public sealed class GraphicsController_StorageHighlight(
        IGraphicsManager graphicsManager,
        State state,
        ActionDispatcher actionDispatcher,
        Input input
    ) : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager = graphicsManager;
        private readonly State _state = state;
        private readonly ActionDispatcher _actionDispatcher = actionDispatcher;
        private readonly Input _input = input;
        private IStoreSettingsParent _currentTarget;
        private IGraphicObject _currentHighlightObject;

        private const float FadeInDuration = 0.15f;
        private const float FadeOutDuration = 0.15f;
        private const float SmoothPaddingDuration = 0.2f;
        private const float TargetBuildingPadding = 0.1f;
        private const float TargetZonePadding = 0.0f;
        private const float DefaultBuildingPadding = 0.2f;
        private const float DefaultZonePadding = 0.1f;

        private static readonly object BuildingHighlightKey =
            typeof(GraphicObject_BuildingHighlight);
        private static readonly object StockpileHighlightKey =
            typeof(GraphicObject_StockpileHighlight);

        private static readonly Color AllowedColor = new(70f / 255f, 203f / 255f, 24f / 255f);
        private static readonly Color DisallowedColor = new(224f / 255f, 60f / 255f, 49f / 255f);

        public void Update()
        {
            var storageUnderMouse = _actionDispatcher.FindStorageAt(Verse.UI.MouseCell());

            bool shouldShow =
                !_input.IsDragging
                && MicrotoolsMod.Settings.directHaulSettings.enableStorageHighlightOnHover
                && storageUnderMouse != null;

            if (shouldShow)
            {
                UpdateHighlightInternal(storageUnderMouse);
            }
            else
            {
                ClearInternal();
            }
        }

        private void UpdateHighlightInternal(IStoreSettingsParent storeSettingsParent)
        {
            if (storeSettingsParent == _currentTarget)
            {
                UpdateExistingHighlightColor();
                return;
            }

            if (_currentHighlightObject != null)
            {
                ClearInternal();
            }

            if (storeSettingsParent == null)
            {
                return;
            }

            _currentTarget = storeSettingsParent;

            Color highlightColor = GetHighlightColorForStorage(
                _state.SelectedThings,
                _currentTarget
            );

            switch (_currentTarget)
            {
                case Building building:
                    EnsureHighlightObject<GraphicObject_BuildingHighlight, Building>(
                        BuildingHighlightKey,
                        building,
                        (b) => new GraphicObject_BuildingHighlight(b),
                        highlightColor,
                        DefaultBuildingPadding,
                        TargetBuildingPadding
                    );
                    break;
                case Zone_Stockpile zone:
                    EnsureHighlightObject<GraphicObject_StockpileHighlight, Zone_Stockpile>(
                        StockpileHighlightKey,
                        zone,
                        (z) => new GraphicObject_StockpileHighlight(z),
                        highlightColor,
                        DefaultZonePadding,
                        TargetZonePadding
                    );
                    break;
                default:
                    _currentTarget = null;
                    break;
            }
        }

        private void UpdateExistingHighlightColor()
        {
            if (_currentHighlightObject is IHasColor colorable && _currentTarget != null)
            {
                colorable.Color = GetHighlightColorForStorage(
                    _state.SelectedThings,
                    _currentTarget
                );
            }
        }

        private void EnsureHighlightObject<TGraphic, TTarget>(
            object key,
            TTarget newTarget,
            Func<TTarget, TGraphic> objectFactory,
            Color color,
            float initialPadding,
            float targetPadding
        )
            where TGraphic : IGraphicObject, IHasPadding, IHasAlpha, IHasColor, IHasTarget<TTarget>
            where TTarget : class
        {
            bool needsEffect = false;
            bool objectFound = _graphicsManager.TryGetGraphicObject(
                key,
                out _currentHighlightObject
            );

            if (!objectFound)
            {
                _currentHighlightObject = objectFactory(newTarget);
                if (_currentHighlightObject is TGraphic highlightObject)
                {
                    highlightObject.Alpha = 0f;
                    highlightObject.Padding = initialPadding;
                    highlightObject.Color = color;
                }
                _graphicsManager.RegisterGraphicObject(_currentHighlightObject);
                needsEffect = true;
            }
            else
            {
                if (_currentHighlightObject is TGraphic highlightObject)
                {
                    highlightObject.Target = newTarget;

                    if (_currentHighlightObject.State == GraphicObjectState.PendingRemoval)
                    {
                        _graphicsManager.RegisterGraphicObject(_currentHighlightObject);
                        highlightObject.Color = color;
                        needsEffect = true;
                    }
                    else
                    {
                        highlightObject.Color = color;
                        needsEffect = true;
                    }
                }
                else
                {
                    _graphicsManager.UnregisterGraphicObject(key);
                    _currentHighlightObject = objectFactory(newTarget);
                    if (_currentHighlightObject is TGraphic newHighlightObject)
                    {
                        newHighlightObject.Alpha = 0f;
                        newHighlightObject.Padding = initialPadding;
                        newHighlightObject.Color = color;
                    }
                    _graphicsManager.RegisterGraphicObject(_currentHighlightObject);
                    needsEffect = true;
                }
            }

            if (needsEffect && _currentHighlightObject is TGraphic targetEffectTarget)
            {
                ApplyHighlightTweens(targetEffectTarget, targetPadding);
            }
        }

        private void ApplyHighlightTweens(IGraphicObject target, float targetPadding)
        {
            if (target?.Key == null)
                return;

            if (target is IHasAlpha alphaTarget)
            {
                _graphicsManager.ApplyTween(
                    target.Key,
                    () => alphaTarget.Alpha,
                    a =>
                    {
                        if (target != null && target is IHasAlpha at)
                            at.Alpha = a;
                    },
                    1f,
                    FadeInDuration,
                    easing: Equations.Linear,
                    propertyId: nameof(IHasAlpha.Alpha)
                );
            }

            if (target is IHasPadding paddingTarget)
            {
                _graphicsManager.ApplyTween(
                    target.Key,
                    () => paddingTarget.Padding,
                    p =>
                    {
                        if (target != null && target is IHasPadding pt)
                            pt.Padding = p;
                    },
                    targetPadding,
                    SmoothPaddingDuration,
                    easing: Equations.Linear,
                    propertyId: nameof(IHasPadding.Padding)
                );
            }
        }

        public void Clear()
        {
            ClearInternal();
        }

        private void ClearInternal()
        {
            if (
                _currentHighlightObject == null
                || _currentHighlightObject.State != GraphicObjectState.Active
            )
            {
                _currentTarget = null;
                return;
            }

            object key = _currentHighlightObject.Key;

            float returnPadding = _currentHighlightObject switch
            {
                GraphicObject_BuildingHighlight => DefaultBuildingPadding,
                GraphicObject_StockpileHighlight => DefaultZonePadding,
                _ => 0f,
            };

            bool tweenStarted = false;
            if (_currentHighlightObject is IHasAlpha alphaTarget && key != null)
            {
                ApplyHighlightAlphaTween(alphaTarget, 0f);
                tweenStarted = true;
            }

            if (_currentHighlightObject is IHasPadding paddingTarget && key != null)
            {
                ApplyHighlightPaddingTween(paddingTarget, returnPadding);
                tweenStarted = true;
            }

            if ((tweenStarted || _currentHighlightObject != null) && key != null)
            {
                _graphicsManager.UnregisterGraphicObject(key);
            }

            if (_currentHighlightObject?.Key == key)
                _currentHighlightObject = null;
            _currentTarget = null;
        }

        private void ApplyHighlightAlphaTween(IHasAlpha alphaTarget, float targetAlpha)
        {
            var graphicObject = alphaTarget as IGraphicObject;
            if (graphicObject?.Key == null)
                return;

            _graphicsManager.ApplyTween(
                graphicObject.Key,
                () => alphaTarget.Alpha,
                a =>
                {
                    if (alphaTarget != null)
                        alphaTarget.Alpha = a;
                },
                targetAlpha,
                targetAlpha == 1f ? FadeInDuration : FadeOutDuration,
                easing: Equations.Linear,
                propertyId: nameof(IHasAlpha.Alpha)
            );
        }

        private void ApplyHighlightPaddingTween(IHasPadding paddingTarget, float targetPadding)
        {
            var graphicObject = paddingTarget as IGraphicObject;
            if (graphicObject?.Key == null)
                return;

            _graphicsManager.ApplyTween(
                graphicObject.Key,
                () => paddingTarget.Padding,
                p =>
                {
                    if (paddingTarget != null)
                        paddingTarget.Padding = p;
                },
                targetPadding,
                SmoothPaddingDuration,
                easing: Equations.Linear,
                propertyId: nameof(IHasPadding.Padding)
            );
        }

        private Color GetHighlightColorForStorage(
            IEnumerable<Thing> selectedThings,
            IStoreSettingsParent storeSettingsParent
        )
        {
            if (storeSettingsParent is null || !selectedThings.Any())
            {
                return Color.white;
            }

            StorageSettings parentSettings = storeSettingsParent.GetParentStoreSettings();
            StorageSettings currentSettings = storeSettingsParent.GetStoreSettings();

            if (parentSettings is null || currentSettings is null)
            {
                return Color.white;
            }

            var validSelectedDefs = selectedThings
                .Select(t => t.def)
                .Where(d => d != null)
                .ToList();
            if (!validSelectedDefs.Any())
            {
                return Color.white;
            }

            bool allDefsFundamentallyAllowed = validSelectedDefs.All(def =>
                parentSettings.filter.Allows(def)
            );

            if (!allDefsFundamentallyAllowed)
            {
                return Color.white;
            }

            bool allCurrentlyAllowed = validSelectedDefs.All(def =>
                currentSettings.filter.Allows(def)
            );

            bool noneCurrentlyAllowed = validSelectedDefs.All(def =>
                !currentSettings.filter.Allows(def)
            );

            if (allCurrentlyAllowed)
            {
                return AllowedColor;
            }
            else if (noneCurrentlyAllowed)
            {
                return DisallowedColor;
            }
            else
            {
                return Color.white;
            }
        }
    }
}
