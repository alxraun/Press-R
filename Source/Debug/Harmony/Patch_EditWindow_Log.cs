using System.Text;
using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace PressR.Debug.Harmony
{
    [HarmonyPatchCategory("Debug")]
    [HarmonyPatch(typeof(EditWindow_Log))]
    internal static class Patch_EditWindow_Log
    {
        [HarmonyPrefix]
        [HarmonyPatch("CopyAllMessagesToClipboard")]
        internal static bool CopyAllMessagesToClipboard_Prefix(EditWindow_Log __instance)
        {
            StringBuilder stringBuilder = new();
            foreach (LogMessage message in Verse.Log.Messages)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.AppendLine();
                stringBuilder.AppendLine(message.ToString());

                if (stringBuilder[stringBuilder.Length - 1] != '\n')
                    stringBuilder.AppendLine();
            }
            GUIUtility.systemCopyBuffer = stringBuilder.ToString();

            return false;
        }
    }
}
