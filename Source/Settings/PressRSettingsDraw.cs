using PressR.UI.Components;
using UnityEngine;

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
