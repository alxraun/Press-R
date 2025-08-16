using HarmonyLib;
using Verse;

namespace PressR.Features.DirectHaul.Harmony
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
            var map = __instance?.MapHeld ?? __instance?.Map;
            if (map == null)
                return true;

            ThingStateManager directHaul =
                map.GetPressRMapComponent()?.DirectHaul?.ThingStateManager;
            if (directHaul == null)
                return true;

            var absorbingThingStatus = directHaul.GetStatus(__instance);

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
