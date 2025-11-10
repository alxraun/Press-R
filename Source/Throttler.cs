using System;
using UnityEngine;

namespace Microtools
{
    public class Throttler
    {
        private readonly float _intervalInSeconds;
        private float _lastExecutionTime;

        private const float TicksPerSecond = 60f;

        public Throttler(int intervalTicks, bool executeImmediatelyFirstTime = true)
        {
            _intervalInSeconds = intervalTicks / TicksPerSecond;
            if (executeImmediatelyFirstTime)
            {
                _lastExecutionTime = -_intervalInSeconds;
            }
            else
            {
                _lastExecutionTime = 0f;
            }
        }

        public bool ShouldExecute()
        {
            float currentTime = Time.realtimeSinceStartup;

            if (_lastExecutionTime == 0f && _intervalInSeconds > 0)
            {
                _lastExecutionTime = currentTime;
            }
            else if (_lastExecutionTime < 0f)
            {
                _lastExecutionTime = currentTime + _lastExecutionTime;
            }

            if (currentTime >= _lastExecutionTime + _intervalInSeconds)
            {
                _lastExecutionTime = currentTime;
                return true;
            }
            return false;
        }

        public void ResetExecutionTime()
        {
            _lastExecutionTime = Time.realtimeSinceStartup;
        }

        public void ForceNextExecutionAndResetInterval()
        {
            _lastExecutionTime = Time.realtimeSinceStartup - _intervalInSeconds;
        }
    }

    public class ThrottledValue<T>
    {
        private readonly Throttler _throttler;
        private readonly Func<T> _valueFactory;
        private T _cachedValue;
        private bool _isPrimed;

        public ThrottledValue(
            int intervalInTicks,
            Func<T> valueFactory,
            bool populateInitialValueOnConstruction = true
        )
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            _throttler = new Throttler(intervalInTicks, true);

            _isPrimed = false;
            _cachedValue = default(T);

            if (populateInitialValueOnConstruction)
            {
                if (_throttler.ShouldExecute())
                {
                    _cachedValue = _valueFactory();
                    _isPrimed = true;
                }
            }
        }

        public T GetValue()
        {
            if (!_isPrimed || _throttler.ShouldExecute())
            {
                _cachedValue = _valueFactory();
                _isPrimed = true;
            }
            return _cachedValue;
        }

        public T ForceRefresh()
        {
            _throttler.ForceNextExecutionAndResetInterval();

            return GetValue();
        }

        public void Invalidate()
        {
            _isPrimed = false;
        }
    }
}
