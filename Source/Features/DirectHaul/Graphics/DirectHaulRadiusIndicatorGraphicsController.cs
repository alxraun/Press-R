using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;
using PressR.Utils.Throttler;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulRadiusIndicatorGraphicsController : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulState _state;
        private ThrottledValue<bool> _isCurrentMouseCellFoggedThrottled;

        private bool _isTemporarilyHidden = false;

        private const float RadiusTweenDuration = 0.35f;
        private const float RadiusPadding = 1.25f;
        private const float FadeInDuration = 0.2f;
        private const float FadeOutDuration = 0.2f;
        private const int FogCheckIntervalTicks = 1;

        private float _lastAppliedTargetRadius;

        private static readonly Color PendingStateColor = new(
            155f / 255f,
            216f / 255f,
            226f / 255f
        );
        private static readonly Color DefaultIndicatorColor = Color.white;
        private static readonly object IndicatorKey =
            typeof(DirectHaulRadiusIndicatorGraphicObject);

        public DirectHaulRadiusIndicatorGraphicsController(
            IGraphicsManager graphicsManager,
            DirectHaulState state
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _isCurrentMouseCellFoggedThrottled = new ThrottledValue<bool>(
                FogCheckIntervalTicks,
                () => _state.CurrentMouseCell.Fogged(_state.Map),
                populateInitialValueOnConstruction: false
            );
        }

        public void Update()
        {
            if (_state.Mode == DirectHaulMode.Storage)
            {
                Clear();
                return;
            }

            bool controllerEnabled = PressRMod.Settings.directHaulSettings.enableRadiusIndicator;

            Color targetColor = GetColorForMode(IsPendingMode());

            bool shouldShowIndicator = DetermineIfIndicatorShouldBeVisible(controllerEnabled);

            _graphicsManager.TryGetGraphicObject(IndicatorKey, out var graphicObject);
            var currentInstance = graphicObject as DirectHaulRadiusIndicatorGraphicObject;

            if (currentInstance != null)
            {
                if (currentInstance.State == GraphicObjectState.Active)
                {
                    currentInstance.Color = targetColor;
                }

                if (shouldShowIndicator)
                {
                    float targetRadius = CalculateTargetRadius(
                        _state.CurrentMouseCell,
                        GetPreviewPositions()
                    );
                    bool needsFadeIn = _isTemporarilyHidden;
                    _isTemporarilyHidden = false;

                    EnsureIndicatorVisible(currentInstance, targetRadius, needsFadeIn);
                }
                else
                {
                    if (!_isTemporarilyHidden && currentInstance.State == GraphicObjectState.Active)
                    {
                        _isTemporarilyHidden = true;
                        ApplyTemporaryFadeOut(currentInstance);
                    }
                }
            }
            else
            {
                if (shouldShowIndicator)
                {
                    float targetRadius = CalculateTargetRadius(
                        _state.CurrentMouseCell,
                        GetPreviewPositions()
                    );

                    CreateAndRegisterIndicator(targetRadius, targetColor);
                    _isTemporarilyHidden = false;
                }
                else
                {
                    _isTemporarilyHidden = false;
                }
            }
        }

        public void Clear()
        {
            if (
                _graphicsManager.TryGetGraphicObject(IndicatorKey, out var graphicObject)
                && graphicObject is DirectHaulRadiusIndicatorGraphicObject instanceToRemove
            )
            {
                InitiateFullRemoval(instanceToRemove);
            }
            _isTemporarilyHidden = false;
            _lastAppliedTargetRadius = 0f;
        }

        private bool DetermineIfIndicatorShouldBeVisible(bool controllerEnabled)
        {
            if (!controllerEnabled || _state.Map == null || _state.IsDragging)
                return false;

            bool hasAnySelected = _state.AllSelectedThings.Any();
            if (!hasAnySelected)
                return false;

            bool hasNonPending = _state.HasAnyNonPendingSelected;

            if (!hasNonPending)
            {
                return false;
            }

            if (!_state.CurrentMouseCell.IsValid)
                return false;

            if (!_state.CurrentMouseCell.InBounds(_state.Map))
                return false;

            if (_isCurrentMouseCellFoggedThrottled.GetValue())
                return false;

            if (_state.CurrentMouseCell.Impassable(_state.Map))
                return false;

            int nonPendingCount = _state.NonPendingSelectedThings.Count;
            var placementCellsFound = _state.CalculatedPlacementCells;
            bool placementPossible =
                placementCellsFound != null && placementCellsFound.Count == nonPendingCount;
            if (!placementPossible)
                return false;

            return true;
        }

        private Dictionary<Thing, IntVec3> GetPreviewPositions()
        {
            var previewPositionsList =
                _state.CalculatedPlacementCells ?? (IReadOnlyList<IntVec3>)Array.Empty<IntVec3>();
            var thingsToConsider = _state.HasAnyNonPendingSelected
                ? _state.NonPendingSelectedThings
                : _state.AllSelectedThings;

            var result = new Dictionary<Thing, IntVec3>();
            int count = Math.Min(previewPositionsList.Count, thingsToConsider.Count);

            for (int i = 0; i < count; i++)
            {
                result[thingsToConsider[i]] = previewPositionsList[i];
            }

            return result;
        }

        private Color GetColorForMode(bool isPending)
        {
            return isPending ? PendingStateColor : DefaultIndicatorColor;
        }

        private bool IsPendingMode()
        {
            return _state.AllSelectedThings.Any() && !_state.HasAnyNonPendingSelected;
        }

        private DirectHaulRadiusIndicatorGraphicObject CreateAndRegisterIndicator(
            float targetRadius,
            Color targetColor
        )
        {
            if (_graphicsManager.TryGetGraphicObject(IndicatorKey, out _))
            {
                return null;
            }

            var newIndicator = new DirectHaulRadiusIndicatorGraphicObject(0f)
            {
                Alpha = 0f,
                Color = targetColor,
            };

            var registeredIndicator =
                _graphicsManager.RegisterGraphicObject(newIndicator)
                as DirectHaulRadiusIndicatorGraphicObject;

            if (registeredIndicator == null)
            {
                return null;
            }

            ApplyFadeInEffect(registeredIndicator);
            ApplyTweenRadius(registeredIndicator, targetRadius, RadiusTweenDuration);
            _lastAppliedTargetRadius = targetRadius;

            return registeredIndicator;
        }

        private void EnsureIndicatorVisible(
            DirectHaulRadiusIndicatorGraphicObject instance,
            float targetRadius,
            bool needsFadeInHint
        )
        {
            if (instance == null)
            {
                return;
            }

            bool justRevived = false;

            if (instance.State == GraphicObjectState.PendingRemoval)
            {
                var revivedInstance =
                    _graphicsManager.RegisterGraphicObject(instance)
                    as DirectHaulRadiusIndicatorGraphicObject;

                if (revivedInstance != null && revivedInstance.State == GraphicObjectState.Active)
                {
                    instance = revivedInstance;
                    justRevived = true;
                }
                else
                {
                    return;
                }
            }

            if (justRevived || needsFadeInHint)
            {
                ApplyFadeInEffect(instance);
            }

            bool radiusChanged = !Mathf.Approximately(targetRadius, _lastAppliedTargetRadius);
            if (justRevived || needsFadeInHint || radiusChanged)
            {
                ApplyTweenRadius(instance, targetRadius, RadiusTweenDuration);
                _lastAppliedTargetRadius = targetRadius;
            }
        }

        private float CalculateTargetRadius(
            IntVec3 mouseCell,
            Dictionary<Thing, IntVec3> previewPositions
        )
        {
            if (previewPositions == null || previewPositions.Count == 0)
            {
                return RadiusPadding;
            }

            Vector3 center = mouseCell.ToVector3Shifted();
            float maxDistSq = 0f;

            foreach (IntVec3 position in previewPositions.Values)
            {
                if (position.IsValid)
                {
                    float distSq = (position.ToVector3Shifted() - center).sqrMagnitude;
                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                    }
                }
            }
            return Mathf.Max(RadiusPadding, Mathf.Sqrt(maxDistSq) + RadiusPadding);
        }

        private void ApplyTweenRadius(
            DirectHaulRadiusIndicatorGraphicObject instance,
            float targetRadius,
            float duration
        )
        {
            if (instance == null || !instance.Key.Equals(IndicatorKey))
                return;

            _graphicsManager.ApplyTween(
                IndicatorKey,
                () => instance.Radius,
                r =>
                {
                    if (
                        _graphicsManager.TryGetGraphicObject(IndicatorKey, out var current)
                        && current == instance
                        && !_isTemporarilyHidden
                    )
                    {
                        instance.Radius = r;
                    }
                },
                targetRadius,
                duration,
                nameof(IHasRadius.Radius),
                easing: Equations.ExpoEaseOut
            );
        }

        private void ApplyFadeInEffect(DirectHaulRadiusIndicatorGraphicObject instance)
        {
            if (instance == null || !instance.Key.Equals(IndicatorKey))
                return;

            _graphicsManager.ApplyTween(
                IndicatorKey,
                () => instance.Alpha,
                a =>
                {
                    if (
                        _graphicsManager.TryGetGraphicObject(IndicatorKey, out var current)
                        && current == instance
                    )
                    {
                        instance.Alpha = a;
                    }
                },
                1f,
                FadeInDuration,
                nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }

        private void ApplyFadeOutEffect(DirectHaulRadiusIndicatorGraphicObject instance)
        {
            if (instance == null || !instance.Key.Equals(IndicatorKey))
                return;

            _graphicsManager.ApplyTween(
                IndicatorKey,
                () => instance.Alpha,
                a =>
                {
                    if (
                        _graphicsManager.TryGetGraphicObject(IndicatorKey, out var current)
                        && current == instance
                    )
                    {
                        instance.Alpha = a;
                    }
                },
                0f,
                FadeOutDuration,
                nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }

        private void ApplyTemporaryFadeOut(DirectHaulRadiusIndicatorGraphicObject instance)
        {
            if (instance == null || instance.State != GraphicObjectState.Active)
            {
                return;
            }
            ApplyFadeOutEffect(instance);
        }

        private void InitiateFullRemoval(DirectHaulRadiusIndicatorGraphicObject instanceToRemove)
        {
            if (instanceToRemove == null || !instanceToRemove.Key.Equals(IndicatorKey))
            {
                _isTemporarilyHidden = false;
                _lastAppliedTargetRadius = 0f;
                return;
            }

            if (instanceToRemove.State == GraphicObjectState.Active)
            {
                ApplyFadeOutEffect(instanceToRemove);
                ApplyTweenRadius(instanceToRemove, 0f, RadiusTweenDuration * 1.20f);
                _graphicsManager.UnregisterGraphicObject(IndicatorKey);
            }

            _isTemporarilyHidden = false;
            _lastAppliedTargetRadius = 0f;
        }
    }
}
