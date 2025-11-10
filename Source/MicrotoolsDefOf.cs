using RimWorld;
using Verse;

namespace Microtools;

[DefOf]
public static class MicrotoolsDefOf
{
    public static KeyBindingDef Microtools_ModifierKey;
    public static WorkGiverDef Microtools_DirectHaul_WorkGiver;
    public static JobDef Microtools_DirectHaul;

    static MicrotoolsDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(MicrotoolsDefOf));
    }
}
