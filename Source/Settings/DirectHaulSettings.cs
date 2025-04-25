using Verse;

namespace PressR.Settings
{
    public class DirectHaulSettings : IExposable
    {
        public const bool EnablePlacementGhostsDefault = true;
        public const bool EnableStatusOverlaysDefault = true;
        public const bool EnableRadiusIndicatorDefault = true;
        public const bool EnableStorageCreationPreviewDefault = true;
        public const bool EnableStorageHighlightOnHoverDefault = true;
        public const bool InvertStandardAndStorageKeysDefault = false;

        public bool enablePlacementGhosts = EnablePlacementGhostsDefault;
        public bool enableStatusOverlays = EnableStatusOverlaysDefault;
        public bool enableRadiusIndicator = EnableRadiusIndicatorDefault;
        public bool enableStorageCreationPreview = EnableStorageCreationPreviewDefault;
        public bool enableStorageHighlightOnHover = EnableStorageHighlightOnHoverDefault;
        public bool invertStandardAndStorageKeys = InvertStandardAndStorageKeysDefault;

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref enablePlacementGhosts,
                "enablePlacementGhosts",
                EnablePlacementGhostsDefault
            );
            Scribe_Values.Look(
                ref enableStatusOverlays,
                "enableStatusOverlays",
                EnableStatusOverlaysDefault
            );
            Scribe_Values.Look(
                ref enableRadiusIndicator,
                "enableRadiusIndicator",
                EnableRadiusIndicatorDefault
            );
            Scribe_Values.Look(
                ref enableStorageCreationPreview,
                "enableStorageCreationPreview",
                EnableStorageCreationPreviewDefault
            );
            Scribe_Values.Look(
                ref enableStorageHighlightOnHover,
                "enableStorageHighlightOnHover",
                EnableStorageHighlightOnHoverDefault
            );
            Scribe_Values.Look(
                ref invertStandardAndStorageKeys,
                "invertStandardAndStorageKeys",
                InvertStandardAndStorageKeysDefault
            );
        }

        public void ResetToDefaults()
        {
            enablePlacementGhosts = EnablePlacementGhostsDefault;
            enableStatusOverlays = EnableStatusOverlaysDefault;
            enableRadiusIndicator = EnableRadiusIndicatorDefault;
            enableStorageCreationPreview = EnableStorageCreationPreviewDefault;
            enableStorageHighlightOnHover = EnableStorageHighlightOnHoverDefault;
            invertStandardAndStorageKeys = InvertStandardAndStorageKeysDefault;
        }
    }
}
