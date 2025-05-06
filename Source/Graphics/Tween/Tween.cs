using System;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Tween
{
    public class Tween<TValue> : ITween
    {
        public Guid Key { get; }
        public string PropertyId { get; }
        public bool IsFinished { get; private set; }
        public Action OnComplete { get; set; }

        private readonly Func<TValue> _getter;
        private readonly Action<TValue> _setter;
        private readonly TValue _endValue;
        private readonly float _duration;
        private readonly EasingFunction _easingFunction;

        private TValue _startValue;
        private float _elapsedTime;
        private bool _isInitialized;
        private bool _killed;

        public Tween(
            Func<TValue> getter,
            Action<TValue> setter,
            TValue endValue,
            float duration,
            string propertyId,
            EasingFunction easing = null
        )
        {
            Key = Guid.NewGuid();
            PropertyId = propertyId ?? throw new ArgumentNullException(nameof(propertyId));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _endValue = endValue;
            _duration = duration > 0f ? duration : 0.001f;
            _easingFunction = easing ?? Equations.Linear;

            _isInitialized = false;
            _elapsedTime = 0f;
            IsFinished = false;
            _killed = false;
        }

        public void Update(float deltaTime)
        {
            if (IsFinished || _killed)
                return;

            if (!_isInitialized)
            {
                _startValue = _getter();
                _isInitialized = true;
            }

            _elapsedTime += deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _duration);
            float easedProgress = _easingFunction(progress);

            TValue currentValue = Interpolate(_startValue, _endValue, easedProgress);
            _setter(currentValue);

            if (_elapsedTime >= _duration)
            {
                CompleteInternal();
            }
        }

        public void Complete()
        {
            if (!IsFinished && !_killed)
            {
                _setter(_endValue);
                CompleteInternal();
            }
        }

        public void Kill()
        {
            if (!IsFinished)
            {
                _killed = true;
                IsFinished = true;
            }
        }

        private void CompleteInternal()
        {
            if (IsFinished)
                return;

            IsFinished = true;
            OnComplete?.Invoke();
        }

        private TValue Interpolate(TValue start, TValue end, float t)
        {
            if (typeof(TValue) == typeof(float))
            {
                return (TValue)
                    (object)Mathf.LerpUnclamped((float)(object)start, (float)(object)end, t);
            }
            if (typeof(TValue) == typeof(Color))
            {
                return (TValue)
                    (object)Color.LerpUnclamped((Color)(object)start, (Color)(object)end, t);
            }
            throw new NotImplementedException(
                $"Interpolation for type {typeof(TValue)} is not implemented."
            );
        }
    }
}
