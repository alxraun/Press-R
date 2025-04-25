using RimWorld;
using UnityEngine;
using Verse;

namespace PressR;

[DefOf]
public static class PressRDefOf
{
    public static KeyBindingDef PressR_ModifierKey;
    public static WorkGiverDef PressR_DirectHaul_WorkGiver;
    public static JobDef PressR_DirectHaul;

    static PressRDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PressRDefOf));
    }
}
