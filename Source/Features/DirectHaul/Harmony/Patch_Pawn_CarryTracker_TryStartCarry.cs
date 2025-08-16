using HarmonyLib;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Harmony
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(
        typeof(Pawn_CarryTracker),
        nameof(Pawn_CarryTracker.TryStartCarry),
        [typeof(Thing), typeof(int), typeof(bool)]
    )]
    public static class Patch_Pawn_CarryTracker_TryStartCarry
    {
        public static void Postfix(Pawn_CarryTracker __instance, int __result)
        {
            if (__result <= 0)
                return;

            Pawn pawn = __instance.pawn;
            if (pawn == null || pawn.Map == null)
                return;

            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def == PressRDefOf.PressR_DirectHaul)
                return;

            Thing carriedThing = __instance.CarriedThing;
            if (carriedThing == null)
                return;

            var map = carriedThing?.MapHeld ?? pawn.Map;
            ThingStateManager directHaul = map
                ?.GetPressRMapComponent()
                ?.DirectHaul?.ThingStateManager;
            if (directHaul == null)
                return;

            directHaul.Remove(carriedThing);
        }
    }
}
