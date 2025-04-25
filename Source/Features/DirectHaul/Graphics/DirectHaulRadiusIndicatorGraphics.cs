using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Graphics.GraphicObjects;
using PressR.Graphics.Effects;
using PressR.Graphics.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulRadiusIndicatorGraphics
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly DirectHaulFrameData _frameData;

        private DirectHaulRadiusIndicatorGraphicObject _radiusIndicatorInstance;

        private const float RadiusChangeSpeed = 12f;
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
        private static object IndicatorKey => typeof(DirectHaulRadiusIndicatorGraphicObject);

        public DirectHaulRadiusIndicatorGraphics(
            IGraphicsManager graphicsManager,
            DirectHaulFrameData frameData
        )
        {
            _graphicsManager =
                graphicsManager ?? throw new System.ArgumentNullException(nameof(graphicsManager));
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        public void UpdateRadiusIndicator(
            IntVec3 mouseCell,
            Dictionary<Thing, IntVec3> previewPositions,
            bool isDragging,
            Map map
        )
        {
            bool shouldShow = ShouldShowIndicator(mouseCell, previewPositions, isDragging, map);
            bool isPendingMode = IsPendingMode();
            Color targetColor = isPendingMode ? PendingStateColor : DefaultIndicatorColor;

            if (_radiusIndicatorInstance != null)
            {
                _radiusIndicatorInstance.Color = targetColor;
            }

            if (shouldShow)
            {
                HandleVisibleIndicator(mouseCell, previewPositions, targetColor);
            }
            else
            {
                HandleHiddenIndicator();
            }
        }

        public void ClearRadiusIndicator()
        {
            if (_radiusIndicatorInstance == null)
            {
                return;
            }

            StopAllEffects(_radiusIndicatorInstance.Key);

            ApplySmoothRadiusEffect(_radiusIndicatorInstance, 0f);
            ApplyFadeOutEffect(_radiusIndicatorInstance);
            _graphicsManager.UnregisterGraphicObject(_radiusIndicatorInstance.Key);

            _lastAppliedTargetRadius = 0f;
            _radiusIndicatorInstance = null;
        }

        private bool ShouldShowIndicator(
            IntVec3 mouseCell,
            Dictionary<Thing, IntVec3> previewPositions,
            bool isDragging,
            Map map
        ) =>
            !isDragging
            && mouseCell.IsValid
            && mouseCell.InBounds(map)
            && !mouseCell.Impassable(map)
            && previewPositions?.Any() == true;

        private bool IsPendingMode()
        {
            bool hasSelectedItems = _frameData.AllSelectedThings.Any();
            bool hasOnlyPending = !_frameData.NonPendingSelectedThings.Any();
            return hasSelectedItems && hasOnlyPending;
        }

        private void HandleVisibleIndicator(
            IntVec3 mouseCell,
            Dictionary<Thing, IntVec3> previewPositions,
            Color targetColor
        )
        {
            float targetRadius = CalculateTargetRadius(mouseCell, previewPositions);

            if (_radiusIndicatorInstance == null)
            {
                CreateIndicator(targetRadius, targetColor);
            }
            else
            {
                EnsureIndicatorIsVisible();

                if (!Mathf.Approximately(targetRadius, _lastAppliedTargetRadius))
                {
                    ApplySmoothRadiusEffect(_radiusIndicatorInstance, targetRadius);
                    _lastAppliedTargetRadius = targetRadius;
                }
            }
        }

        private void HandleHiddenIndicator()
        {
            if (
                _radiusIndicatorInstance != null
                && !_graphicsManager
                    .GetEffectsForTarget(_radiusIndicatorInstance.Key)
                    .OfType<FadeOutEffect>()
                    .Any()
            )
            {
                StartIndicatorFadeOut();
            }
        }

        private void CreateIndicator(float initialRadius, Color initialColor)
        {
            StopAllEffects(IndicatorKey);

            var newIndicator = new DirectHaulRadiusIndicatorGraphicObject(0f)
            {
                Alpha = 0f,
                Color = initialColor,
            };

            if (_graphicsManager.RegisterGraphicObject(newIndicator))
            {
                _radiusIndicatorInstance = newIndicator;
                ApplySmoothRadiusEffect(newIndicator, initialRadius);
                ApplyFadeInEffect(newIndicator);
                _lastAppliedTargetRadius = initialRadius;
            }
            else
            {
                _radiusIndicatorInstance = null;
            }
        }

        private void EnsureIndicatorIsVisible()
        {
            if (_radiusIndicatorInstance == null)
                return;

            if (StopEffectsOfType<FadeOutEffect>(_radiusIndicatorInstance.Key))
            {
                ApplyFadeInEffect(_radiusIndicatorInstance);
            }
            else if (
                !_graphicsManager
                    .GetEffectsForTarget(_radiusIndicatorInstance.Key)
                    .OfType<FadeInEffect>()
                    .Any()
                && _radiusIndicatorInstance.Alpha < 1f
            )
            {
                ApplyFadeInEffect(_radiusIndicatorInstance);
            }
        }

        private void StartIndicatorFadeOut()
        {
            if (_radiusIndicatorInstance == null)
                return;

            StopEffectsOfType<FadeInEffect>(_radiusIndicatorInstance.Key);
            StopSmoothRadiusEffects(_radiusIndicatorInstance.Key);
            ApplyFadeOutEffect(_radiusIndicatorInstance);
        }

        private float CalculateTargetRadius(
            IntVec3 mouseCell,
            Dictionary<Thing, IntVec3> previewPositions
        )
        {
            if (previewPositions?.Any() != true)
                return 0f;

            Vector3 center = mouseCell.ToVector3Shifted();
            float maxDistSq = previewPositions
                .Values.Where(p => p.IsValid)
                .Select(p => (p.ToVector3Shifted() - center).sqrMagnitude)
                .DefaultIfEmpty(0f)
                .Max();

            return Mathf.Approximately(maxDistSq, 0f)
                ? RadiusPadding
                : Mathf.Sqrt(maxDistSq) + RadiusPadding;
        }

        private void ApplySmoothRadiusEffect(
            DirectHaulRadiusIndicatorGraphicObject target,
            float targetRadius,
            float speed = RadiusChangeSpeed
        )
        {
            if (target == null)
                return;
            StopSmoothRadiusEffects(target.Key);
            var effectInstance = new SmoothRadiusEffect(targetRadius, speed);
            _graphicsManager.ApplyEffect(new List<object> { target.Key }, effectInstance);
        }

        private void ApplyFadeInEffect(IGraphicObject target, float duration = FadeInDuration)
        {
            if (target == null)
                return;
            StopEffectsOfType<FadeOutEffect>(target.Key);
            if (!_graphicsManager.GetEffectsForTarget(target.Key).OfType<FadeInEffect>().Any())
            {
                _graphicsManager.ApplyEffect(new[] { target.Key }, new FadeInEffect(duration));
            }
        }

        private void ApplyFadeOutEffect(IGraphicObject target, float duration = FadeOutDuration)
        {
            if (target == null)
                return;
            StopEffectsOfType<FadeInEffect>(target.Key);
            if (!_graphicsManager.GetEffectsForTarget(target.Key).OfType<FadeOutEffect>().Any())
            {
                var effect = new FadeOutEffect(duration);
                _graphicsManager.ApplyEffect(new[] { target.Key }, effect);
            }
        }

        private void StopSmoothRadiusEffects(object targetKey) =>
            StopEffectsOfType<SmoothRadiusEffect>(targetKey);

        private void StopAllEffects(object targetKey)
        {
            var effectsToStop = _graphicsManager.GetEffectsForTarget(targetKey).ToList();
            effectsToStop.ForEach(effect => _graphicsManager.StopEffect(effect.Key));
        }

        private bool StopEffectsOfType<T>(object targetKey)
            where T : IEffect
        {
            var effectsToStop = _graphicsManager
                .GetEffectsForTarget(targetKey)
                .Where(effect => effect is T)
                .ToList();

            if (!effectsToStop.Any())
            {
                return false;
            }

            effectsToStop.ForEach(effect => _graphicsManager.StopEffect(effect.Key));
            return true;
        }
    }
}
