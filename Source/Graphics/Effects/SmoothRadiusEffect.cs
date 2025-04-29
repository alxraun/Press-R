using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Graphics.GraphicObjects;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Effects
{
    public class SmoothRadiusEffect : IEffect
    {
        private readonly float _targetRadius;
        private readonly float _speed;
        private readonly List<IGraphicObject> _targets = new List<IGraphicObject>();
        private const float Threshold = 0.01f;

        public Guid Key { get; } = Guid.NewGuid();
        public List<IGraphicObject> Targets => _targets.ToList();
        public EffectState State { get; set; } = EffectState.Active;

        public bool IsFinished
        {
            get
            {

                return !_targets.Any()
                    || _targets.All(t =>
                        t is IHasRadius radiusTarget
                        && Mathf.Abs(radiusTarget.Radius - _targetRadius) < Threshold
                    );
            }
        }

        public SmoothRadiusEffect(float targetRadius, float speed = 8f)
        {
            _targetRadius = Mathf.Max(0f, targetRadius);
            _speed = speed;
        }

        public void Update(float deltaTime)
        {
            if (State != EffectState.Active)
                return;

            foreach (IGraphicObject target in _targets)
            {
                if (!(target is IHasRadius radiusTarget))
                {
                    continue;
                }

                radiusTarget.Radius = Mathf.Lerp(
                    radiusTarget.Radius,
                    _targetRadius,
                    deltaTime * _speed
                );
            }

            if (IsFinished)
            {
                State = EffectState.PendingRemoval;
            }
        }

        public void OnAttach(IGraphicObject target)
        {
            if (target is IHasRadius && target is IEffectTarget && !_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        public void OnDetach(IGraphicObject target)
        {
            _targets.Remove(target);
            if (!_targets.Any())
            {
                State = EffectState.PendingRemoval;
            }
        }

        public object Clone()
        {
            return new SmoothRadiusEffect(_targetRadius, _speed);
        }
    }
}
