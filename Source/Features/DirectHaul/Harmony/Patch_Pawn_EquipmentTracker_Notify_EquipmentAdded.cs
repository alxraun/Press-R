using HarmonyLib;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Harmony
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(
        typeof(Pawn_EquipmentTracker),
        nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded)
    )]
    public static class Patch_Pawn_EquipmentTracker_Notify_EquipmentAdded
    {
        public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null || eq == null || pawn.Map == null)
                return;

            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def == PressRDefOf.PressR_DirectHaul)
                return;

            var map = eq?.MapHeld ?? pawn.Map;
            ThingStateManager directHaul = map
                ?.GetPressRMapComponent()
                ?.DirectHaul?.ThingStateManager;
            if (directHaul == null)
                return;

            directHaul.Remove(eq);
        }
    }
}
