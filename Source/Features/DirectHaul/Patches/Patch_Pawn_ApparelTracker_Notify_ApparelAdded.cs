using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Patches
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded))]
    public static class Patch_Pawn_ApparelTracker_Notify_ApparelAdded
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null || apparel == null || pawn.Map == null)
                return;

            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def == PressRDefOf.PressR_DirectHaul)
                return;

            PressRMapComponent mapComponent = pawn.Map.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;
            if (directHaulData == null)
                return;

            directHaulData.RemoveThingFromTracking(apparel);
        }
    }
}
