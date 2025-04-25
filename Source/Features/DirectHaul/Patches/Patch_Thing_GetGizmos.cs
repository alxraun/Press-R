using System.Collections.Generic;
using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Gizmos;
using Verse;

namespace PressR.Features.DirectHaul.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
    public static class Patch_Thing_GetGizmos
    {
        public static IEnumerable<Verse.Gizmo> Postfix(
            IEnumerable<Verse.Gizmo> __result,
            Thing __instance
        )
        {
            foreach (var gizmo in __result)
            {
                yield return gizmo;
            }

            if (__instance == null || !__instance.Spawned || __instance.Destroyed)
            {
                yield break;
            }

            PressRMapComponent mapComponent = __instance.Map?.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;

            if (directHaulData == null)
            {
                yield break;
            }

            DirectHaulStatus status = directHaulData.GetStatusForThing(__instance);

            if (status == DirectHaulStatus.Held)
            {
                yield return new CancelHeldStatusGizmo(__instance, directHaulData);
            }
            else if (status == DirectHaulStatus.Pending)
            {
                yield return new CancelPendingStatusGizmo(__instance, directHaulData);
            }
        }
    }
}
