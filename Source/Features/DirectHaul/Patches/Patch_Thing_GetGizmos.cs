using System.Collections.Generic;
using HarmonyLib;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Gizmos;
using Verse;

namespace PressR.Features.DirectHaul.Patches
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
    public static class Patch_Thing_GetGizmos
    {
        public static IEnumerable<Verse.Gizmo> Postfix(
            IEnumerable<Verse.Gizmo> __result,
            Thing __instance
        )
        {
            var list = __result is List<Gizmo> l ? l : new List<Gizmo>(__result);

            if (__instance == null || !__instance.Spawned || __instance.Destroyed)
                return list;

            PressRMapComponent mapComponent = __instance.Map?.GetComponent<PressRMapComponent>();
            DirectHaulExposableData directHaulData = mapComponent?.DirectHaulExposableData;

            if (directHaulData == null)
                return list;

            DirectHaulStatus status = directHaulData.GetStatusForThing(__instance);

            if (status == DirectHaulStatus.Held)
                list.Add(new CancelHeldStatusGizmo(__instance, directHaulData));
            else if (status == DirectHaulStatus.Pending)
                list.Add(new CancelPendingStatusGizmo(__instance, directHaulData));

            return list;
        }
    }
}
