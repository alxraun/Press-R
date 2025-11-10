using System.Runtime.CompilerServices;
using Verse;

namespace Microtools
{
    public static class MicrotoolsMapComponentCache
    {
        private static readonly ConditionalWeakTable<
            Map,
            MicrotoolsMapComponent
        > _microtoolsCache = [];
        private static System.WeakReference<Map> _lastMapRef;
        private static System.WeakReference<MicrotoolsMapComponent> _lastCompRef;

        public static MicrotoolsMapComponent GetMicrotoolsMapComponent(this Map map)
        {
            return GetMicrotoolsInternal(map);
        }

        public static MicrotoolsMapComponent GetMicrotoolsMapComponent(this Thing thing)
        {
            var map = thing.MapHeld ?? thing.Map;
            return GetMicrotoolsInternal(map);
        }

        private static MicrotoolsMapComponent GetMicrotoolsInternal(Map map)
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

            if (_microtoolsCache.TryGetValue(map, out var cached))
            {
                _lastMapRef = new System.WeakReference<Map>(map);
                _lastCompRef = new System.WeakReference<MicrotoolsMapComponent>(cached);
                return cached;
            }

            var resolved = map.GetComponent<MicrotoolsMapComponent>();
            if (resolved != null)
            {
                _microtoolsCache.Add(map, resolved);
                _lastMapRef = new System.WeakReference<Map>(map);
                _lastCompRef = new System.WeakReference<MicrotoolsMapComponent>(resolved);
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
