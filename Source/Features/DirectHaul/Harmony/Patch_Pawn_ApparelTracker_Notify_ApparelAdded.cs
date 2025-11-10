using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Microtools.Features.DirectHaul.Harmony
{
    [HarmonyPatchCategory("Microtools")]
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded))]
    public static class Patch_Pawn_ApparelTracker_Notify_ApparelAdded
    {
        public static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null || apparel == null || pawn.Map == null)
                return;

            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def == MicrotoolsDefOf.Microtools_DirectHaul)
                return;

            var map = apparel?.MapHeld ?? pawn.Map;
            ThingStateManager directHaul = map
                ?.GetMicrotoolsMapComponent()
                ?.DirectHaul?.ThingStateManager;
            if (directHaul == null)
                return;

            directHaul.Remove(apparel);
        }
    }
}
