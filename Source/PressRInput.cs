using System;
using PressR.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR
{
    public static class PressRInput
    {
        public static bool IsModifierIncrement10xKeyPressed =>
            KeyBindingDefOf.ModifierIncrement_10x.IsDown;
        public static bool IsModifierIncrement100xKeyPressed =>
            KeyBindingDefOf.ModifierIncrement_100x.IsDown;
        public static bool IsMouseButtonDown =>
            Event.current.type == EventType.MouseDown && Event.current.button == 0;
        public static bool IsMouseButtonHeld => Input.GetMouseButton(0);
        public static bool IsMouseButtonUp =>
            Event.current.type == EventType.MouseUp && Event.current.button == 0;
        public static bool IsPressRModifierKeyPressed => PressRDefOf.PressR_ModifierKey.IsDown;
    }
}
