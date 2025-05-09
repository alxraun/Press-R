using LudeonTK;
using RimWorld;
using Verse;

namespace PressR.Debug.ValueMonitor
{
    public static class ValueMonitorActions
    {
        [DebugAction(
            "Press-R",
            "ValueMonitor Window",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        private static void ToggleValueMonitorWindow()
        {
            ValueMonitorWindow.ToggleWindow();
        }
    }
}
