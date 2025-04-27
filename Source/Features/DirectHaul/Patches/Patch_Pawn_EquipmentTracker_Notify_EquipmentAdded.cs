using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Patches
{
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

            PressRMapComponent mapComponent = pawn.Map.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;
            if (directHaulData == null)
                return;

            directHaulData.RemoveThingFromTracking(eq);
        }
    }
}
