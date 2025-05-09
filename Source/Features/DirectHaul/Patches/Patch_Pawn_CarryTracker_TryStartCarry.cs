using System;
using System.Linq;
using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Patches
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

            PressRMapComponent mapComponent = pawn.Map.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;
            if (directHaulData == null)
                return;

            directHaulData.RemoveThingFromTracking(carriedThing);
        }
    }
}
