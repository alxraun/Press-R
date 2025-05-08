using System;
using UnityEngine;
using Verse;

namespace PressR.Utils.Throttler
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
}
