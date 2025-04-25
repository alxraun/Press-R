using System.Collections.Generic;
using PressR.UI.Components;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Settings
{
    public class PressRSettingsDraw
    {
        public void DrawSettings(Rect rect, PressRSettings settings)
        {
            ModSettingsContainer.Draw(rect, settings);
        }
    }
}
