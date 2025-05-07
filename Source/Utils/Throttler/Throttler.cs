using System;
using Verse;

namespace PressR.Utils.Throttler
{
    public class Throttler
    {
        private readonly int _intervalInTicks;
        private int _lastExecutionTick;

        public Throttler(int intervalInTicks, bool executeImmediatelyFirstTime = true)
        {
            _intervalInTicks = intervalInTicks > 0 ? intervalInTicks : 1;
            _lastExecutionTick = executeImmediatelyFirstTime
                ? GenTicks.TicksGame - _intervalInTicks
                : GenTicks.TicksGame;
        }

        public bool ShouldExecute()
        {
            int currentTick = GenTicks.TicksGame;
            if (currentTick >= _lastExecutionTick + _intervalInTicks)
            {
                _lastExecutionTick = currentTick;
                return true;
            }
            return false;
        }

        public void ResetExecutionTime()
        {
            _lastExecutionTick = GenTicks.TicksGame;
        }

        public void ForceNextExecutionAndResetInterval()
        {
            _lastExecutionTick = GenTicks.TicksGame - _intervalInTicks;
        }
    }
}
