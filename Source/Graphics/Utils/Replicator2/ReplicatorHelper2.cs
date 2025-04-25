using System.Reflection;
using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2
{
    public static class ReplicatorHelper2
    {
        private const float DefaultMaxRandomAngle = 35f;
        private const float RandomAngleMultiplier = 542f;

        private static FieldInfo _graphicRandomRotatedMaxAngleField;

        static ReplicatorHelper2()
        {
            try
            {
                _graphicRandomRotatedMaxAngleField = typeof(Graphic_RandomRotated).GetField(
                    "maxAngle",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );

                if (_graphicRandomRotatedMaxAngleField == null) { }
            }
            catch (System.Exception) { }
        }

        public static float GetRandomRotationAngle(Thing thing)
        {
            if (thing?.Graphic is not Graphic_RandomRotated graphicRandomRotated)
            {
                return 0f;
            }

            float maxAngle = DefaultMaxRandomAngle;
            if (_graphicRandomRotatedMaxAngleField != null)
            {
                try
                {
                    maxAngle = (float)
                        _graphicRandomRotatedMaxAngleField.GetValue(graphicRandomRotated);
                }
                catch (System.Exception) { }
            }

            if (maxAngle <= 0f)
                maxAngle = DefaultMaxRandomAngle;

            float angleRange = maxAngle * 2.0f;
            float deterministicValue = (thing.thingIDNumber * RandomAngleMultiplier) % angleRange;

            return -maxAngle + deterministicValue;
        }
    }
}
