using LudeonTK;
using RimWorld;
using Verse;

namespace PressR.Debugger
{
    public static class DebuggerActions
    {
#if DEBUG
        [DebugAction(
            "Press-R",
            "Debugger Window",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        private static void ToggleDebuggerWindow()
        {
            DebuggerWindow.ToggleWindow();
        }
#endif
    }
}
