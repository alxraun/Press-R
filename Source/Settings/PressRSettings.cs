using Verse;

namespace PressR.Settings
{
    public class PressRSettings : ModSettings
    {
        public const bool EnableDirectHaulDefault = true;
        public const bool EnableTabLensDefault = true;

        public bool enableDirectHaul = EnableDirectHaulDefault;
        public bool enableTabLens = EnableTabLensDefault;

        public TabLensSettings tabLensSettings = new TabLensSettings();
        public DirectHaulSettings directHaulSettings = new DirectHaulSettings();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableDirectHaul, "enableDirectHaul", EnableDirectHaulDefault);
            Scribe_Values.Look(ref enableTabLens, "enableTabLens", EnableTabLensDefault);

            if (tabLensSettings == null)
                tabLensSettings = new TabLensSettings();
            if (directHaulSettings == null)
                directHaulSettings = new DirectHaulSettings();

            Scribe_Deep.Look(ref tabLensSettings, "tabLensSettings");
            Scribe_Deep.Look(ref directHaulSettings, "directHaulSettings");

            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (tabLensSettings == null)
                {
                    Log.Warning("PressR TabLensSettings failed to load, resetting to defaults.");
                    tabLensSettings = new TabLensSettings();
                }
                if (directHaulSettings == null)
                {
                    Log.Warning("PressR DirectHaulSettings failed to load, resetting to defaults.");
                    directHaulSettings = new DirectHaulSettings();
                }
            }
        }

        public void ResetToDefaults()
        {
            enableDirectHaul = EnableDirectHaulDefault;
            enableTabLens = EnableTabLensDefault;

            if (tabLensSettings == null)
                tabLensSettings = new TabLensSettings();
            else
                tabLensSettings.ResetToDefaults();

            if (directHaulSettings == null)
                directHaulSettings = new DirectHaulSettings();
            else
                directHaulSettings.ResetToDefaults();
        }
    }
}
