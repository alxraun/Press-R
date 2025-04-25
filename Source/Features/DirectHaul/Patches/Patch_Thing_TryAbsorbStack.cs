using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.Patches
{
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

            bool isAbsorbingThingHeld =
                directHaulData.GetStatusForThing(__instance) == DirectHaulStatus.Held;

            bool isAbsorptionAllowedByDirectHaul = false;

            if (other.ParentHolder is Pawn_CarryTracker carryTracker && carryTracker.pawn != null)
            {
                Pawn carrierPawn = carryTracker.pawn;
                Job curJob = carrierPawn.CurJob;

                if (curJob != null && curJob.def == PressRDefOf.PressR_DirectHaul)
                {
                    if (curJob.GetTarget(TargetIndex.B).Cell == __instance.Position)
                    {
                        isAbsorptionAllowedByDirectHaul = true;
                    }
                }
            }

            if (isAbsorbingThingHeld && !isAbsorptionAllowedByDirectHaul)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
