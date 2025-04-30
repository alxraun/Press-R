using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Graphics.GraphicObjects;
using UnityEngine;

namespace PressR.Graphics.Effects
{
    public class SmoothPaddingEffect : IEffect
    {
        private readonly float _targetPadding;
        private readonly float _speed;
        private readonly List<IGraphicObject> _targets = new List<IGraphicObject>();
        private const float Threshold = 0.01f;

        public Guid Key { get; } = Guid.NewGuid();
        public List<IGraphicObject> Targets => _targets.ToList();
        public EffectState State { get; set; } = EffectState.Active;

        public float TargetPadding => _targetPadding;

        public bool IsFinished
        {
            get
            {
                return !_targets.Any()
                    || _targets.All(t =>
                        t is IHasPadding paddingTarget
                        && Mathf.Abs(paddingTarget.Padding - _targetPadding) < Threshold
                    );
            }
        }

        public SmoothPaddingEffect(float targetPadding, float speed = 8f)
        {
            _targetPadding = Mathf.Max(0f, targetPadding);
            _speed = speed;
        }

        public void Update(float deltaTime)
        {
            if (State != EffectState.Active)
                return;

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                IGraphicObject target = _targets[i];

                if (!(target is IHasPadding paddingTarget) || !(target is IEffectTarget))
                {
                    _targets.RemoveAt(i);
                    continue;
                }

                paddingTarget.Padding = Mathf.Lerp(
                    paddingTarget.Padding,
                    _targetPadding,
                    deltaTime * _speed
                );
            }

            if (IsFinished || !_targets.Any())
            {
                State = EffectState.PendingRemoval;
            }
        }

        public void OnAttach(IGraphicObject target)
        {
            if (target is IHasPadding && target is IEffectTarget && !_targets.Contains(target))
            {
                _targets.Add(target);
                State = EffectState.Active;
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
            return new SmoothPaddingEffect(_targetPadding, _speed);
        }
    }
}
