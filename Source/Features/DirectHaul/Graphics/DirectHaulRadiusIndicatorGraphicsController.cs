using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DirectHaulRadiusIndicatorGraphicsController
        : IGraphicsController<DirectHaulUpdateContext>
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulFrameData _frameData;

        private bool _isTemporarilyHidden = false;

        private const float RadiusTweenDuration = 0.35f;
        private const float RadiusPadding = 1.25f;
        private const float FadeInDuration = 0.2f;
        private const float FadeOutDuration = 0.2f;

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
            DirectHaulFrameData frameData
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            _frameData = frameData ?? throw new ArgumentNullException(nameof(frameData));
        }

        public void Update(DirectHaulUpdateContext context)
        {
            if (context.Mode == DirectHaulMode.Storage)
            {
                Clear();
                return;
            }

            bool controllerEnabled = PressRMod.Settings.directHaulSettings.enableRadiusIndicator;

            Color targetColor = GetColorForMode(IsPendingMode());

            bool shouldShowIndicator = DetermineIfIndicatorShouldBeVisible(
                context,
                controllerEnabled
            );

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
                        context.CurrentMouseCell,
                        GetPreviewPositions(context)
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
                        context.CurrentMouseCell,
                        GetPreviewPositions(context)
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

        private bool DetermineIfIndicatorShouldBeVisible(
            DirectHaulUpdateContext context,
            bool controllerEnabled
        )
        {
            if (!controllerEnabled || context.Map == null || context.DragState.IsDragging)
                return false;

            bool hasAnySelected = context.FrameData.AllSelectedThings.Any();
            if (!hasAnySelected)
                return false;

            bool hasNonPending = context.FrameData.NonPendingSelectedThings.Any();

            if (!hasNonPending)
            {
                return false;
            }

            bool canShowBasedOnCell =
                context.CurrentMouseCell.IsValid
                && context.CurrentMouseCell.InBounds(context.Map)
                && !context.CurrentMouseCell.Impassable(context.Map);
            if (!canShowBasedOnCell)
                return false;

            int nonPendingCount = context.FrameData.NonPendingSelectedThings.Count;
            var placementCellsFound = context.FrameData.CalculatedPlacementCells;
            bool placementPossible =
                placementCellsFound != null && placementCellsFound.Count == nonPendingCount;
            if (!placementPossible)
                return false;

            return true;
        }

        private Dictionary<Thing, IntVec3> GetPreviewPositions(DirectHaulUpdateContext context)
        {
            var previewPositionsList =
                context.FrameData.CalculatedPlacementCells ?? Array.Empty<IntVec3>();
            var thingsToConsider = context.FrameData.NonPendingSelectedThings.Any()
                ? context.FrameData.NonPendingSelectedThings
                : context.FrameData.AllSelectedThings;
            return previewPositionsList
                .Select((cell, index) => new { Cell = cell, Index = index })
                .Where(pair => pair.Index < thingsToConsider.Count)
                .ToDictionary(pair => thingsToConsider[pair.Index], pair => pair.Cell);
        }

        private Color GetColorForMode(bool isPending)
        {
            return isPending ? PendingStateColor : DefaultIndicatorColor;
        }

        private bool IsPendingMode()
        {
            return _frameData.AllSelectedThings.Any() && !_frameData.NonPendingSelectedThings.Any();
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
            if (previewPositions?.Any() != true)
                return RadiusPadding;
            Vector3 center = mouseCell.ToVector3Shifted();
            float maxDistSq = previewPositions
                .Values.Where(p => p.IsValid)
                .Select(p => (p.ToVector3Shifted() - center).sqrMagnitude)
                .DefaultIfEmpty(0f)
                .Max();
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
