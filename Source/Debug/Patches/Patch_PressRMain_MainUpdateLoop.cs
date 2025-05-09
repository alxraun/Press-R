using HarmonyLib;
using PressR.Debug.ValueMonitor;
using UnityEngine;
using Verse;

namespace PressR.Debug.Patches
{
    [HarmonyPatchCategory("Debug")]
    [HarmonyPatch(typeof(PressRMain), nameof(PressRMain.MainUpdateLoop))]
    public static class Patch_PressRMain_MainUpdateLoop
    {
        public static void Postfix()
        {
            ValueMonitorCore.Initialize();

            if (Current.ProgramState == ProgramState.Playing && ValueMonitorWindow.IsWindowOpen)
            {
                ValueMonitorCore.Tick(Time.deltaTime);
            }
        }
    }
}
