using System.Collections.Generic;
using System.Linq;
using Microtools.Graphics;
using Microtools.Graphics.Tween;
using UnityEngine;
using Verse;

namespace Microtools.Features.DirectHaul
{
    public sealed class GraphicsController_RadiusIndicator
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly State _state;
        private readonly ThingStateManager _thingStateManager;
        private readonly Input _input;
        private readonly ThrottledValue<bool> _isCurrentMouseCellFoggedThrottled;

        private bool _isTemporarilyHidden;

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
        private static readonly object IndicatorKey = typeof(GraphicObject_RadiusIndicator);

        public GraphicsController_RadiusIndicator(
            IGraphicsManager graphicsManager,
            State state,
            ThingStateManager thingStateManager,
            Input input
        )
        {
            _graphicsManager = graphicsManager;
            _state = state;
            _thingStateManager = thingStateManager;
            _input = input;
            _isCurrentMouseCellFoggedThrottled = new ThrottledValue<bool>(
                FogCheckIntervalTicks,
                () => _state.CurrentMap != null && Verse.UI.MouseCell().Fogged(_state.CurrentMap),
                false
            );
        }

        public void Update()
        {
            var controllerEnabled = MicrotoolsMod.Settings.directHaulSettings.enableRadiusIndicator;

            var targetColor = GetColorForMode(IsPendingMode());

            var shouldShowIndicator = DetermineIfIndicatorShouldBeVisible(controllerEnabled);

            _graphicsManager.TryGetGraphicObject(IndicatorKey, out var graphicObject);

            if (graphicObject is GraphicObject_RadiusIndicator currentInstance)
            {
                if (currentInstance.State == GraphicObjectState.Active)
                {
                    currentInstance.Color = targetColor;
                }

                if (shouldShowIndicator)
                {
                    var targetRadius = CalculateTargetRadius(
                        Verse.UI.MouseCell(),
                        GetPreviewPositions()
                    );
                    var needsFadeIn = _isTemporarilyHidden;
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
                    var targetRadius = CalculateTargetRadius(
                        Verse.UI.MouseCell(),
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
                && graphicObject is GraphicObject_RadiusIndicator instanceToRemove
            )
            {
                InitiateFullRemoval(instanceToRemove);
            }
            _isTemporarilyHidden = false;
            _lastAppliedTargetRadius = 0f;
        }

        private bool DetermineIfIndicatorShouldBeVisible(bool controllerEnabled)
        {
            if (!controllerEnabled || _state.CurrentMap == null || _input.IsDragging)
            {
                return false;
            }

            var untrackedThings = _thingStateManager.UntrackedSelectedThings;
            if (!untrackedThings.Any())
            {
                return false;
            }

            var mouseCell = Verse.UI.MouseCell();
            if (!mouseCell.IsValid || !mouseCell.InBounds(_state.CurrentMap))
            {
                return false;
            }

            if (_isCurrentMouseCellFoggedThrottled.GetValue())
            {
                return false;
            }

            if (mouseCell.Impassable(_state.CurrentMap))
            {
                return false;
            }

            if (mouseCell.InNoZoneEdgeArea(_state.CurrentMap))
            {
                return false;
            }

            var placementPossible = _state.GhostPlacements.Count == untrackedThings.Count();
            if (!placementPossible)
            {
                return false;
            }

            return true;
        }

        private Dictionary<Thing, IntVec3> GetPreviewPositions()
        {
            return _state.GhostPlacements;
        }

        private Color GetColorForMode(bool isPending)
        {
            return isPending ? PendingStateColor : DefaultIndicatorColor;
        }

        private bool IsPendingMode()
        {
            return _state.SelectedThings.Any() && !_thingStateManager.UntrackedSelectedThings.Any();
        }

        private GraphicObject_RadiusIndicator CreateAndRegisterIndicator(
            float targetRadius,
            Color targetColor
        )
        {
            if (_graphicsManager.TryGetGraphicObject(IndicatorKey, out _))
            {
                return null;
            }

            var newIndicator = new GraphicObject_RadiusIndicator(0f)
            {
                Alpha = 0f,
                Color = targetColor,
            };

            if (
                _graphicsManager.RegisterGraphicObject(newIndicator)
                is not GraphicObject_RadiusIndicator registeredIndicator
            )
            {
                return null;
            }

            ApplyFadeIn(registeredIndicator);
            ApplyTweenRadius(registeredIndicator, targetRadius, RadiusTweenDuration);
            _lastAppliedTargetRadius = targetRadius;

            return registeredIndicator;
        }

        private void EnsureIndicatorVisible(
            GraphicObject_RadiusIndicator instance,
            float targetRadius,
            bool needsFadeInHint
        )
        {
            if (instance == null)
            {
                return;
            }

            var justRevived = false;

            if (instance.State == GraphicObjectState.PendingRemoval)
            {
                if (
                    _graphicsManager.RegisterGraphicObject(instance)
                        is GraphicObject_RadiusIndicator revivedInstance
                    && revivedInstance.State == GraphicObjectState.Active
                )
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
                ApplyFadeIn(instance);
            }

            var radiusChanged = !Mathf.Approximately(targetRadius, _lastAppliedTargetRadius);
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

            var center = mouseCell.ToVector3Shifted();
            var maxDistSq = 0f;

            foreach (var position in previewPositions.Values)
            {
                if (position.IsValid)
                {
                    var distSq = (position.ToVector3Shifted() - center).sqrMagnitude;
                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                    }
                }
            }
            return Mathf.Max(RadiusPadding, Mathf.Sqrt(maxDistSq) + RadiusPadding);
        }

        private void ApplyTweenRadius(
            GraphicObject_RadiusIndicator instance,
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

        private void ApplyFadeIn(GraphicObject_RadiusIndicator instance)
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

        private void ApplyFadeOut(GraphicObject_RadiusIndicator instance)
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

        private void ApplyTemporaryFadeOut(GraphicObject_RadiusIndicator instance)
        {
            if (instance == null || instance.State != GraphicObjectState.Active)
            {
                return;
            }
            ApplyFadeOut(instance);
        }

        private void InitiateFullRemoval(GraphicObject_RadiusIndicator instanceToRemove)
        {
            if (instanceToRemove == null || !instanceToRemove.Key.Equals(IndicatorKey))
            {
                _isTemporarilyHidden = false;
                _lastAppliedTargetRadius = 0f;
                return;
            }

            if (instanceToRemove.State == GraphicObjectState.Active)
            {
                ApplyFadeOut(instanceToRemove);
                ApplyTweenRadius(instanceToRemove, 0f, RadiusTweenDuration * 1.20f);
                _graphicsManager.UnregisterGraphicObject(IndicatorKey);
            }

            _isTemporarilyHidden = false;
            _lastAppliedTargetRadius = 0f;
        }
    }
}
