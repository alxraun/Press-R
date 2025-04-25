using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    internal static class ReplicatorHelper
    {
        public static float GetRandomRotationAngle(
            Graphic graphic,
            Thing thing,
            float defaultMaxAngle,
            float randomAngleMultiplier
        )
        {
            if (thing == null || !(graphic is Graphic_RandomRotated graphicRandomRotated))
            {
                return 0f;
            }

            float maxAngle = defaultMaxAngle;
            FieldInfo maxAngleFieldInfo = null;

            try
            {
                maxAngleFieldInfo = typeof(Graphic_RandomRotated).GetField(
                    "maxAngle",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );

                if (maxAngleFieldInfo != null)
                {
                    maxAngle = (float)maxAngleFieldInfo.GetValue(graphicRandomRotated);
                }
            }
            catch (System.Exception) { }

            if (maxAngle <= 0f)
                maxAngle = defaultMaxAngle;
            float angleRange = maxAngle * 2.0f;

            float deterministicValue = (thing.thingIDNumber * randomAngleMultiplier) % angleRange;
            return -maxAngle + deterministicValue;
        }
    }
}
