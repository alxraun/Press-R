using System.Collections.Generic;
using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Gizmos;
using RimWorld;
using Verse;

namespace PressR.Features.DirectHaul.Patches
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(typeof(StoreUtility), nameof(StoreUtility.TryFindBestBetterStoreCellFor))]
    public static class Patch_StoreUtility_TryFindBestBetterStoreCellFor
    {
        public static bool Prefix(ref bool __result, Thing t, Pawn carrier, Map map)
        {
            PressRMapComponent mapComponent = map?.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;

            if (directHaulData == null)
            {
                return true;
            }

            var status = directHaulData.GetStatusForThing(t);

            if (status != DirectHaulStatus.Held && status != DirectHaulStatus.Pending)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
