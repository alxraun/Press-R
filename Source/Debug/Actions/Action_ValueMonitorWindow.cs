using LudeonTK;
using PressR.Debug.ValueMonitor;

namespace PressR.Debug.Actions
{
    public static class ValueMonitorActions
    {
        [DebugAction(
            "Press-R",
            "Value Monitor",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        private static void ToggleValueMonitorWindow()
        {
            ValueMonitorWindow.ToggleWindow();
        }
    }
}
