using Microtools.Settings;
using UnityEngine;
using Verse;
using static Microtools.UI.Components.Constants;

namespace Microtools.UI.Components
{
    public static class ModSettingsContainer
    {
        private static Vector2 scrollPosition = Vector2.zero;

        public static void Draw(Rect rect, MicrotoolsSettings settings)
        {
            Listing_Standard listingStandard = new();
            Rect contentRect = rect.ContractedBy(10f);
            float estimatedViewHeight = 750f;
            Rect viewRect = new(0f, 0f, contentRect.width - 16f, estimatedViewHeight);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
            listingStandard.Begin(viewRect);

            FeatureSection.Draw(
                listingStandard,
                ref settings.storageLensSettings.enableStorageLens,
                label: "Microtools.Settings.TabLens.StorageLens.Enable.Label".Translate(),
                description: "Microtools.Settings.TabLens.StorageLens.Enable.Description".Translate(),
                drawContentAction: (sectionListing, nextIndentLevel) =>
                {
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.TabLens.StorageLens.Overlays.Enable.Label".Translate(),
                        description: "Microtools.Settings.TabLens.StorageLens.Overlays.Enable.Description".Translate(),
                        currentValue: settings.storageLensSettings.enableStorageLensOverlays,
                        onValueChanged: (newValue) =>
                            settings.storageLensSettings.enableStorageLensOverlays = newValue,
                        indentLevel: nextIndentLevel
                    );
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.TabLens.StorageLens.RestoreUI.Enable.Label".Translate(),
                        description: "Microtools.Settings.TabLens.StorageLens.RestoreUI.Enable.Description".Translate(),
                        currentValue: settings.storageLensSettings.restoreUIStateOnDeactivate,
                        onValueChanged: (newValue) =>
                            settings.storageLensSettings.restoreUIStateOnDeactivate = newValue,
                        indentLevel: nextIndentLevel
                    );
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.TabLens.StorageLens.AutoOpenTab.Enable.Label".Translate(),
                        description: "Microtools.Settings.TabLens.StorageLens.AutoOpenTab.Enable.Description".Translate(),
                        currentValue: settings.storageLensSettings.openStorageTabAutomatically,
                        onValueChanged: (newValue) =>
                            settings.storageLensSettings.openStorageTabAutomatically = newValue,
                        indentLevel: nextIndentLevel
                    );
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.TabLens.StorageLens.FocusOnClick.Enable.Label".Translate(),
                        description: "Microtools.Settings.TabLens.StorageLens.FocusOnClick.Enable.Description".Translate(),
                        currentValue: settings.storageLensSettings.FocusItemInTabOnClick,
                        onValueChanged: (newValue) =>
                            settings.storageLensSettings.FocusItemInTabOnClick = newValue,
                        indentLevel: nextIndentLevel
                    );

                    bool isFocusOnClickEnabled = settings.storageLensSettings.FocusItemInTabOnClick;
                    string hoverDescription = isFocusOnClickEnabled
                        ? "Microtools.Settings.TabLens.StorageLens.FocusOnHover.Enable.Description.Enabled".Translate()
                        : "Microtools.Settings.TabLens.StorageLens.FocusOnHover.Enable.Description.Disabled".Translate();

                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.TabLens.StorageLens.FocusOnHover.Enable.Label".Translate(),
                        description: hoverDescription,
                        currentValue: settings.storageLensSettings.FocusItemInTabOnHover,
                        onValueChanged: (newValue) =>
                            settings.storageLensSettings.FocusItemInTabOnHover = newValue,
                        disabled: !isFocusOnClickEnabled,
                        indentLevel: nextIndentLevel
                    );
                }
            );

            listingStandard.Gap(SectionGap);

            FeatureSection.Draw(
                listingStandard,
                ref settings.enableDirectHaul,
                label: "Microtools.Settings.DirectHaul.Feature.Label".Translate(),
                description: "Microtools.Settings.DirectHaul.Feature.Description".Translate(),
                drawContentAction: (sectionListing, nextIndentLevel) =>
                {
                    /*
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.DirectHaul.PlacementGhosts.Enable.Label".Translate(),
                        description: "Microtools.Settings.DirectHaul.PlacementGhosts.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enablePlacementGhosts,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enablePlacementGhosts = newValue,
                        indentLevel: nextIndentLevel
                    );
                    */
                    /*
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.DirectHaul.StatusOverlays.Enable.Label".Translate(),
                        description: "Microtools.Settings.DirectHaul.StatusOverlays.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableStatusOverlays,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableStatusOverlays = newValue,
                        indentLevel: nextIndentLevel
                    );
                    */
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.DirectHaul.RadiusIndicator.Enable.Label".Translate(),
                        description: "Microtools.Settings.DirectHaul.RadiusIndicator.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableRadiusIndicator,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableRadiusIndicator = newValue,
                        indentLevel: nextIndentLevel
                    );
                    /*
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.DirectHaul.StorageCreationPreview.Enable.Label".Translate(),
                        description: "Microtools.Settings.DirectHaul.StorageCreationPreview.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableStorageCreationPreview,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableStorageCreationPreview = newValue,
                        indentLevel: nextIndentLevel
                    );
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.DirectHaul.StorageHighlight.Enable.Label".Translate(),
                        description: "Microtools.Settings.DirectHaul.StorageHighlight.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableStorageHighlightOnHover,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableStorageHighlightOnHover = newValue,
                        indentLevel: nextIndentLevel
                    );
                    */
                    SettingsItem.Draw(
                        sectionListing,
                        label: "Microtools.Settings.DirectHaul.InvertKeys.Enable.Label".Translate(),
                        description: "Microtools.Settings.DirectHaul.InvertKeys.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.invertStandardAndStorageKeys,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.invertStandardAndStorageKeys = newValue,
                        indentLevel: nextIndentLevel
                    );
                }
            );

            listingStandard.Gap(SectionGap);

            SettingsResetButton.Draw(listingStandard, settings);

            listingStandard.End();
            Widgets.EndScrollView();
        }
    }
}
