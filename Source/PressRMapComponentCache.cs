using System.Runtime.CompilerServices;
using Verse;

namespace PressR
{
    public static class PressRMapComponentCache
    {
        private static readonly ConditionalWeakTable<Map, PressRMapComponent> _pressrCache = [];
        private static System.WeakReference<Map> _lastMapRef;
        private static System.WeakReference<PressRMapComponent> _lastCompRef;

        public static PressRMapComponent GetPressRMapComponent(this Map map)
        {
            return GetPressRInternal(map);
        }

        public static PressRMapComponent GetPressRMapComponent(this Thing thing)
        {
            var map = thing.MapHeld ?? thing.Map;
            return GetPressRInternal(map);
        }

        private static PressRMapComponent GetPressRInternal(Map map)
        {
            if (map == null)
            {
                return null;
            }

            if (
                _lastMapRef != null
                && _lastMapRef.TryGetTarget(out var lastMap)
                && ReferenceEquals(map, lastMap)
            )
            {
                if (_lastCompRef != null && _lastCompRef.TryGetTarget(out var lastComp))
                {
                    return lastComp;
                }
            }

            if (_pressrCache.TryGetValue(map, out var cached))
            {
                _lastMapRef = new System.WeakReference<Map>(map);
                _lastCompRef = new System.WeakReference<PressRMapComponent>(cached);
                return cached;
            }

            var resolved = map.GetComponent<PressRMapComponent>();
            if (resolved != null)
            {
                _pressrCache.Add(map, resolved);
                _lastMapRef = new System.WeakReference<Map>(map);
                _lastCompRef = new System.WeakReference<PressRMapComponent>(resolved);
            }
            else
            {
                _lastMapRef = null;
                _lastCompRef = null;
            }
            return resolved;
        }
    }
}
