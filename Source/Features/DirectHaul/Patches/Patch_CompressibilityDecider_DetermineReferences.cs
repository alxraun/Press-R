using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using Verse;

namespace PressR.Features.DirectHaul.Patches
{
    [HarmonyPatch(typeof(CompressibilityDecider), "DetermineReferences")]
    public static class Patch_CompressibilityDecider_DetermineReferences
    {
        private static readonly FieldInfo ReferencedThingsField = AccessTools.Field(
            typeof(CompressibilityDecider),
            "referencedThings"
        );
        private static readonly FieldInfo MapField = AccessTools.Field(
            typeof(CompressibilityDecider),
            "map"
        );

        public static void Postfix(CompressibilityDecider __instance)
        {
            if (ReferencedThingsField == null || MapField == null)
            {
                return;
            }

            var referencedThings = (HashSet<Thing>)ReferencedThingsField.GetValue(__instance);
            var map = (Map)MapField.GetValue(__instance);

            if (referencedThings == null || map == null)
            {
                return;
            }

            DirectHaulExposableData directHaulData =
                map.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
            if (directHaulData == null)
            {
                return;
            }

            IEnumerable<Thing> trackedThings = directHaulData.GetAllTrackedThings();
            if (trackedThings != null)
            {
                foreach (Thing thing in trackedThings)
                {
                    if (thing != null && !thing.Destroyed)
                    {
                        referencedThings.Add(thing);
                    }
                }
            }
        }
    }
}
