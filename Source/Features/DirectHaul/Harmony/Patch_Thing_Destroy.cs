using HarmonyLib;
using Verse;

namespace PressR.Features.DirectHaul.Harmony
{
    [HarmonyPatchCategory("PressR")]
    [HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
    public static class Patch_Thing_Destroy
    {
        public static void Prefix(Thing __instance, out Map __state)
        {
            __state = __instance?.MapHeld;
        }

        public static void Postfix(Thing __instance, Map __state)
        {
            if (__state == null || __instance == null)
            {
                return;
            }

            var directHaul = __state.GetPressRMapComponent()?.DirectHaul?.ThingStateManager;
            if (directHaul == null)
            {
                return;
            }

            if (directHaul.GetStatus(__instance) != DirectHaulStatus.None)
            {
                directHaul.Remove(__instance);
            }
        }
    }
}
