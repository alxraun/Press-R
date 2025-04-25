using System;
using System.Collections.Generic;
using Verse;

namespace PressR.Gizmo
{
    public static class GizmosDisablingManager
    {
        private static bool _isDisablingGloballyActive = false;
        private static string _currentDisablerReason = null;
        private static HashSet<Type> _exclusions = new HashSet<Type>();

        public static bool IsDisablingActive => _isDisablingGloballyActive;

        public static string CurrentReason => _currentDisablerReason;

        public static void EnableGlobalDisabling(string reason, IEnumerable<Type> exclusions = null)
        {
            _isDisablingGloballyActive = true;
            _currentDisablerReason = reason ?? string.Empty;
            _exclusions.Clear();
            if (exclusions != null)
            {
                foreach (var type in exclusions)
                {
                    if (typeof(Verse.Gizmo).IsAssignableFrom(type))
                    {
                        _exclusions.Add(type);
                    }
                }
            }
        }

        public static void DisableGlobalDisabling()
        {
            _isDisablingGloballyActive = false;
            _currentDisablerReason = null;
            _exclusions.Clear();
        }

        public static bool IsExcluded(Type gizmoType)
        {
            Type currentType = gizmoType;
            while (currentType != null && typeof(Verse.Gizmo).IsAssignableFrom(currentType))
            {
                if (_exclusions.Contains(currentType))
                {
                    return true;
                }
                currentType = currentType.BaseType;
            }
            return false;
        }
    }
}
