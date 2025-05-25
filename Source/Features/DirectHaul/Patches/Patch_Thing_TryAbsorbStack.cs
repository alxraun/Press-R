using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Patches
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(typeof(Thing), nameof(Thing.TryAbsorbStack))]
    public static class Patch_Thing_TryAbsorbStack
    {
        public static bool Prefix(
            Thing __instance,
            Thing other,
            bool respectStackLimit,
            ref bool __result
        )
        {
            if (__instance?.Map == null)
                return true;

            PressRMapComponent mapComponent = __instance.Map.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;
            if (directHaulData == null)
                return true;

            DirectHaulStatus absorbingThingStatus = directHaulData.GetStatusForThing(__instance);

            if (absorbingThingStatus != DirectHaulStatus.Held)
            {
                return true;
            }

            bool isOtherCarriedByPawn = other.ParentHolder is Pawn_CarryTracker;

            if (isOtherCarriedByPawn)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
