using Verse;

namespace PressR.Settings
{
    public class PressRSettings : ModSettings
    {
        public const bool EnableDirectHaulDefault = true;
        public const bool EnableTabLensDefault = true;

        public bool enableDirectHaul = EnableDirectHaulDefault;
        public bool enableTabLens = EnableTabLensDefault;

        public StorageLensSettings storageLensSettings = new();
        public DirectHaulSettings directHaulSettings = new();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableDirectHaul, "enableDirectHaul", EnableDirectHaulDefault);
            Scribe_Values.Look(ref enableTabLens, "enableTabLens", EnableTabLensDefault);

            storageLensSettings ??= new StorageLensSettings();
            directHaulSettings ??= new DirectHaulSettings();

            Scribe_Deep.Look(ref storageLensSettings, "tabLensSettings");
            Scribe_Deep.Look(ref directHaulSettings, "directHaulSettings");

            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (storageLensSettings == null)
                {
                    Log.Warning("PressR TabLensSettings failed to load, resetting to defaults.");
                    storageLensSettings = new StorageLensSettings();
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

            if (storageLensSettings == null)
                storageLensSettings = new StorageLensSettings();
            else
                storageLensSettings.ResetToDefaults();

            if (directHaulSettings == null)
                directHaulSettings = new DirectHaulSettings();
            else
                directHaulSettings.ResetToDefaults();
        }
    }
}
