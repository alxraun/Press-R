using HarmonyLib;
using PressR.Settings;
using UnityEngine;
using Verse;

namespace PressR
{
    public class PressRMod : Mod
    {
        public static PressRSettings Settings { get; private set; }

        private readonly PressRSettingsDraw _settingsUI = new PressRSettingsDraw();

        public PressRMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<PressRSettings>();

            var harmony = new Harmony("Alx.PressR");

#if DEBUG
            Harmony.DEBUG = true;
#endif

            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Press-R Alpha";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            _settingsUI.DrawSettings(inRect, Settings);

            base.DoSettingsWindowContents(inRect);
        }
    }
}
