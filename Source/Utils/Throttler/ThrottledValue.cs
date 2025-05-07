using System;
using Verse;

namespace PressR.Utils.Throttler
{
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
