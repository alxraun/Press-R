using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Graphics.GraphicObjects;
using PressR.Graphics;
using PressR.Graphics.Effects;
using PressR.Graphics.GraphicObjects;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStorageHighlightGraphics
    {
        private readonly IGraphicsManager _graphicsManager;
        private IStoreSettingsParent _currentTarget;
        private IGraphicObject _currentHighlightObject;
        private Guid _currentFadeInEffectId = Guid.Empty;
        private Guid _currentFadeOutEffectId = Guid.Empty;
        private Guid _currentSmoothPaddingEffectId = Guid.Empty;

        private const float FadeInDuration = 0.15f;
        private const float FadeOutDuration = 0.15f;
        private const float SmoothPaddingSpeed = 10f;
        private const float TargetBuildingPadding = 0.1f;
        private const float TargetZonePadding = 0.0f;
        private const float DefaultBuildingPadding = 0.2f;
        private const float DefaultZonePadding = 0.1f;

        private static readonly object BuildingHighlightKey =
            typeof(BuildingHighlightGraphicObject);
        private static readonly object ZoneHighlightKey = typeof(ZoneHighlightGraphicObject);

        public DirectHaulStorageHighlightGraphics(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
        }

        public void UpdateHighlight(
            IStoreSettingsParent storeSettingsParent,
            Map map,
            DirectHaulFrameData frameData
        )
        {
            if (storeSettingsParent == _currentTarget)
            {
                UpdateExistingHighlightColor(frameData);
                return;
            }

            if (_currentHighlightObject != null)
            {
                ClearHighlight();
            }

            if (storeSettingsParent == null)
            {
                return;
            }

            _currentTarget = storeSettingsParent;

            Color highlightColor = GetHighlightColorForStorage(
                frameData.AllSelectedThings,
                _currentTarget
            );

            switch (_currentTarget)
            {
                case Building building:
                    EnsureHighlightObject<BuildingHighlightGraphicObject, Building>(
                        BuildingHighlightKey,
                        building,
                        (b) => new BuildingHighlightGraphicObject(b),
                        highlightColor,
                        DefaultBuildingPadding,
                        TargetBuildingPadding
                    );
                    break;
                case Zone_Stockpile zone:
                    EnsureHighlightObject<ZoneHighlightGraphicObject, Zone_Stockpile>(
                        ZoneHighlightKey,
                        zone,
                        (z) => new ZoneHighlightGraphicObject(z),
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

        private void UpdateExistingHighlightColor(DirectHaulFrameData frameData)
        {
            if (_currentHighlightObject is IHasColor colorable && _currentTarget != null)
            {
                colorable.Color = GetHighlightColorForStorage(
                    frameData.AllSelectedThings,
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
            where TGraphic : IGraphicObject,
                IEffectTarget,
                IHasPadding,
                IHasAlpha,
                IHasColor,
                IHasTarget<TTarget>
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
                        needsEffect =
                            _currentFadeInEffectId == Guid.Empty
                            || !_graphicsManager
                                .GetEffectsForTarget(key)
                                .Any(e =>
                                    e.Key == _currentFadeInEffectId && e.State == EffectState.Active
                                );
                    }
                }
                else
                {
                    _graphicsManager.UnregisterGraphicObject(key, force: true);
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
                ApplyEffects(targetEffectTarget, targetPadding);
            }
        }

        private void ApplyEffects(IEffectTarget target, float targetPadding)
        {
            StopEffects();

            _currentFadeInEffectId = _graphicsManager.ApplyEffect(
                new[] { _currentHighlightObject.Key },
                new FadeInEffect(FadeInDuration)
            );
            _currentSmoothPaddingEffectId = _graphicsManager.ApplyEffect(
                new[] { _currentHighlightObject.Key },
                new SmoothPaddingEffect(targetPadding, SmoothPaddingSpeed)
            );
        }

        public void ClearHighlight()
        {
            if (
                _currentHighlightObject == null
                || _currentHighlightObject.State == GraphicObjectState.PendingRemoval
            )
            {
                _currentTarget = null;
                return;
            }

            StopEffects();

            float returnPadding = _currentHighlightObject switch
            {
                BuildingHighlightGraphicObject => DefaultBuildingPadding,
                ZoneHighlightGraphicObject => DefaultZonePadding,
                _ => 0f,
            };

            _currentFadeOutEffectId = _graphicsManager.ApplyEffect(
                new[] { _currentHighlightObject.Key },
                new FadeOutEffect(FadeOutDuration)
            );

            _currentSmoothPaddingEffectId = _graphicsManager.ApplyEffect(
                new[] { _currentHighlightObject.Key },
                new SmoothPaddingEffect(returnPadding, SmoothPaddingSpeed)
            );

            _graphicsManager.UnregisterGraphicObject(_currentHighlightObject.Key);

            _currentHighlightObject = null;
            _currentTarget = null;
        }

        private void StopEffects()
        {
            TryStopEffect(_currentFadeInEffectId);
            TryStopEffect(_currentFadeOutEffectId);
            TryStopEffect(_currentSmoothPaddingEffectId);
            _currentFadeInEffectId = Guid.Empty;
            _currentFadeOutEffectId = Guid.Empty;
            _currentSmoothPaddingEffectId = Guid.Empty;
        }

        private void TryStopEffect(Guid effectId)
        {
            if (effectId != Guid.Empty)
            {
                _graphicsManager.StopEffect(effectId);
            }
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
                return GraphicsUtils.GetAllowedColor();
            }
            else if (noneCurrentlyAllowed)
            {
                return GraphicsUtils.GetDisallowedColor();
            }
            else
            {
                return Color.white;
            }
        }
    }
}
