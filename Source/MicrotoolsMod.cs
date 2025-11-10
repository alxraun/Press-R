using HarmonyLib;
using Microtools.Settings;
using UnityEngine;
using Verse;

namespace Microtools
{
    public class MicrotoolsMod : Mod
    {
        public static MicrotoolsSettings Settings { get; private set; }

        private readonly MicrotoolsSettingsDraw _settingsUI = new();

        public MicrotoolsMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<MicrotoolsSettings>();

            var harmony = new Harmony("Alx.Microtools");

            harmony.PatchCategory(typeof(MicrotoolsMod).Assembly, "Microtools");

#if DEBUG
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                harmony.PatchCategory(typeof(MicrotoolsMod).Assembly, "Debug");
            });
#endif
        }

        public override string SettingsCategory() => "Microtools Alpha";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            _settingsUI.DrawSettings(inRect, Settings);

            base.DoSettingsWindowContents(inRect);
        }
    }
}
