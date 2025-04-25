using System.Reflection;
using Verse;

namespace PressR.Utils
{
    public static class ReflectionUtils
    {
        public static T GetFieldValue<T>(
            object obj,
            string fieldName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
                return default(T);
            var field = obj.GetType().GetField(fieldName, bindingFlags);
            return field != null ? (T)field.GetValue(obj) : default(T);
        }

        public static T GetPropertyValue<T>(
            object obj,
            string propertyName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
                return default(T);
            var property = obj.GetType().GetProperty(propertyName, bindingFlags);
            return property != null ? (T)property.GetValue(obj) : default;
        }

        public static PropertyInfo GetPropertyInfo(
            object obj,
            string propertyName,
            BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public
        )
        {
            if (obj == null)
                return null;
            return obj.GetType().GetProperty(propertyName, bindingFlags);
        }
    }
}
