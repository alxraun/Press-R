using HarmonyLib;
using RimWorld;
using Verse;

namespace PressR.Features.DirectHaul.Harmony
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(typeof(StoreUtility), nameof(StoreUtility.TryFindBestBetterStoreCellFor))]
    public static class Patch_StoreUtility_TryFindBestBetterStoreCellFor
    {
        public static bool Prefix(ref bool __result, Thing t, Pawn carrier, Map map)
        {
            var componentMap = t?.MapHeld ?? map;
            ThingStateManager directHaul = componentMap
                ?.GetPressRMapComponent()
                ?.DirectHaul?.ThingStateManager;

            if (directHaul == null)
            {
                return true;
            }

            var status = directHaul.GetStatus(t);

            if (status != DirectHaulStatus.Held && status != DirectHaulStatus.Pending)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
