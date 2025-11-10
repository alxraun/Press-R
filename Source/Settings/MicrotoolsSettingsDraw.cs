using Microtools.UI.Components;
using UnityEngine;

namespace Microtools.Settings
{
    public class MicrotoolsSettingsDraw
    {
        public void DrawSettings(Rect rect, MicrotoolsSettings settings)
        {
            ModSettingsContainer.Draw(rect, settings);
        }
    }
}
