using System.Reflection;
using RimWorld;
using Verse;

namespace PressR.Utils
{
    public static class UIUtils
    {
        public static T GetActiveITabOfType<T>()
            where T : ITab
        {
            MainTabWindow_Inspect inspector =
                MainButtonDefOf.Inspect.TabWindow as MainTabWindow_Inspect;
            if (inspector == null || inspector.CurTabs == null)
            {
                return null;
            }
            foreach (InspectTabBase tab in inspector.CurTabs)
            {
                if (tab is T foundTab)
                    return foundTab;
            }
            return null;
        }
    }
}
