using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Graphics.GraphicObjects;
using UnityEngine;

namespace PressR.Graphics.Effects
{
    public class FadeInEffect : IEffect
    {
        private readonly Guid _id;
        private readonly float _duration;
        private float _startTime = -1f;
        private readonly Dictionary<IGraphicObject, float> _initialAlphas =
            new Dictionary<IGraphicObject, float>();
        private readonly float _targetAlpha;

        public List<IGraphicObject> Targets { get; } = new List<IGraphicObject>();
        public EffectState State { get; set; }

        public Guid Key => _id;

        public bool IsFinished =>
            _duration == 0 || (_startTime >= 0 && (Time.time - _startTime) >= _duration);

        public FadeInEffect(float duration, float targetAlpha = 1.0f)
        {
            _id = Guid.NewGuid();
            _duration = duration > 0 ? duration : 0f;
            _targetAlpha = Mathf.Clamp01(targetAlpha);
            State = EffectState.Active;
        }

        public FadeInEffect(float duration, Guid id, float targetAlpha = 1.0f)
        {
            _id = id;
            _duration = duration > 0 ? duration : 0f;
            _targetAlpha = Mathf.Clamp01(targetAlpha);
            State = EffectState.Active;
        }

        public void Update(float deltaTime)
        {
            if (State != EffectState.Active)
                return;

            if (_startTime < 0)
                _startTime = Time.time;

            if (IsFinished)
            {
                foreach (var target in Targets.ToList())
                {
                    if (target is IHasAlpha alphaTarget)
                    {
                        alphaTarget.Alpha = _targetAlpha;
                    }
                }
                return;
            }

            float elapsedTime = Time.time - _startTime;
            float progress = Mathf.Clamp01(elapsedTime / _duration);

            foreach (var target in Targets.ToList())
            {
                if (target is IHasAlpha alphaTarget)
                {
                    if (_initialAlphas.TryGetValue(target, out float initialAlphaForTarget))
                    {
                        alphaTarget.Alpha =
                            initialAlphaForTarget
                            + (_targetAlpha - initialAlphaForTarget) * progress;
                    }
                    else
                    {
                        alphaTarget.Alpha = 0f + (_targetAlpha - 0f) * progress;
                    }
                }
            }
        }

        public void OnAttach(IGraphicObject target)
        {
            if (target is IHasAlpha alphaTarget)
            {
                if (!_initialAlphas.ContainsKey(target))
                {
                    _initialAlphas.Add(target, alphaTarget.Alpha);
                }
                else
                {
                    _initialAlphas[target] = alphaTarget.Alpha;
                }
            }

            if (
                Targets.Count(t => t.State == GraphicObjectState.Active) == 1
                && Targets.Contains(target)
            )
            {
                _startTime = -1f;
            }
        }

        public void OnDetach(IGraphicObject target)
        {
            _initialAlphas.Remove(target);
        }
    }
}
