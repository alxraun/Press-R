using System.Collections.Generic;
using HarmonyLib;
using Microtools.Features.DirectHaul;
using Verse;

namespace Microtools.Features.DirectHaul.Harmony
{
    [HarmonyPatchCategory("Microtools")]
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
    public static class Patch_Thing_GetGizmos
    {
        public static IEnumerable<Verse.Gizmo> Postfix(
            IEnumerable<Verse.Gizmo> __result,
            Thing __instance
        )
        {
            var list = __result is List<Gizmo> l ? l : [.. __result];

            if (__instance == null || !__instance.Spawned || __instance.Destroyed)
                return list;

            var map = __instance?.MapHeld ?? __instance?.Map;
            ThingStateManager directHaul = map
                ?.GetMicrotoolsMapComponent()
                ?.DirectHaul?.ThingStateManager;

            if (directHaul == null)
                return list;

            var status = directHaul.GetStatus(__instance);

            switch (status)
            {
                case DirectHaulStatus.Held:
                    list.Add(new Command_CancelHeldStatus(__instance, directHaul));
                    break;
                case DirectHaulStatus.Pending:
                    list.Add(new Command_CancelPendingStatus(__instance, directHaul));
                    break;
            }

            return list;
        }
    }
}
