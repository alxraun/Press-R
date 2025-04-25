using PressR.Settings;
using UnityEngine;
using Verse;
using static PressR.UI.Components.Constants;

namespace PressR.UI.Components
{
    public static class ModSettingsContainer
    {
        private static Vector2 scrollPosition = Vector2.zero;

        public static void Draw(Rect rect, PressRSettings settings)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            Rect contentRect = rect.ContractedBy(10f);
            float estimatedViewHeight = 750f;
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, estimatedViewHeight);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);
            listingStandard.Begin(viewRect);

            FeatureSection.Draw(
                listingStandard,
                ref settings.enableTabLens,
                label: "PressR.Settings.TabLens.Feature.Label".Translate(),
                description: "PressR.Settings.TabLens.Feature.Description".Translate(),
                drawContentAction: (sectionListing, nextIndentLevel) =>
                {
                    bool tabLensEnabled = settings.enableTabLens;

                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.TabLens.StorageLens.Enable.Label".Translate(),
                        description: "PressR.Settings.TabLens.StorageLens.Enable.Description".Translate(),
                        currentValue: settings.tabLensSettings.enableStorageLens,
                        onValueChanged: (newValue) =>
                            settings.tabLensSettings.enableStorageLens = newValue,
                        disabled: !tabLensEnabled,
                        indentLevel: nextIndentLevel
                    );

                    if (settings.tabLensSettings.enableStorageLens)
                    {
                        SettingsItem.Draw(
                            sectionListing,
                            label: "PressR.Settings.TabLens.StorageLens.Overlays.Enable.Label".Translate(),
                            description: "PressR.Settings.TabLens.StorageLens.Overlays.Enable.Description".Translate(),
                            currentValue: settings.tabLensSettings.enableStorageLensOverlays,
                            onValueChanged: (newValue) =>
                                settings.tabLensSettings.enableStorageLensOverlays = newValue,
                            disabled: !tabLensEnabled,
                            indentLevel: nextIndentLevel
                        );
                        SettingsItem.Draw(
                            sectionListing,
                            label: "PressR.Settings.TabLens.StorageLens.RestoreUI.Enable.Label".Translate(),
                            description: "PressR.Settings.TabLens.StorageLens.RestoreUI.Enable.Description".Translate(),
                            currentValue: settings.tabLensSettings.restoreUIStateOnDeactivate,
                            onValueChanged: (newValue) =>
                                settings.tabLensSettings.restoreUIStateOnDeactivate = newValue,
                            disabled: !tabLensEnabled,
                            indentLevel: nextIndentLevel
                        );
                        SettingsItem.Draw(
                            sectionListing,
                            label: "PressR.Settings.TabLens.StorageLens.AutoOpenTab.Enable.Label".Translate(),
                            description: "PressR.Settings.TabLens.StorageLens.AutoOpenTab.Enable.Description".Translate(),
                            currentValue: settings.tabLensSettings.openStorageTabAutomatically,
                            onValueChanged: (newValue) =>
                                settings.tabLensSettings.openStorageTabAutomatically = newValue,
                            disabled: !tabLensEnabled,
                            indentLevel: nextIndentLevel
                        );
                        SettingsItem.Draw(
                            sectionListing,
                            label: "PressR.Settings.TabLens.StorageLens.FocusOnClick.Enable.Label".Translate(),
                            description: "PressR.Settings.TabLens.StorageLens.FocusOnClick.Enable.Description".Translate(),
                            currentValue: settings.tabLensSettings.FocusItemInTabOnClick,
                            onValueChanged: (newValue) =>
                                settings.tabLensSettings.FocusItemInTabOnClick = newValue,
                            disabled: !tabLensEnabled,
                            indentLevel: nextIndentLevel
                        );

                        bool isFocusOnClickEnabled = settings.tabLensSettings.FocusItemInTabOnClick;
                        string hoverDescription = isFocusOnClickEnabled
                            ? "PressR.Settings.TabLens.StorageLens.FocusOnHover.Enable.Description.Enabled".Translate()
                            : "PressR.Settings.TabLens.StorageLens.FocusOnHover.Enable.Description.Disabled".Translate();

                        SettingsItem.Draw(
                            sectionListing,
                            label: "PressR.Settings.TabLens.StorageLens.FocusOnHover.Enable.Label".Translate(),
                            description: hoverDescription,
                            currentValue: settings.tabLensSettings.FocusItemInTabOnHover,
                            onValueChanged: (newValue) =>
                                settings.tabLensSettings.FocusItemInTabOnHover = newValue,
                            disabled: !tabLensEnabled || !isFocusOnClickEnabled,
                            indentLevel: nextIndentLevel
                        );
                    }
                }
            );

            listingStandard.Gap(SectionGap);

            FeatureSection.Draw(
                listingStandard,
                ref settings.enableDirectHaul,
                label: "PressR.Settings.DirectHaul.Feature.Label".Translate(),
                description: "PressR.Settings.DirectHaul.Feature.Description".Translate(),
                drawContentAction: (sectionListing, nextIndentLevel) =>
                {
                    /*
                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.DirectHaul.PlacementGhosts.Enable.Label".Translate(),
                        description: "PressR.Settings.DirectHaul.PlacementGhosts.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enablePlacementGhosts,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enablePlacementGhosts = newValue,
                        indentLevel: nextIndentLevel
                    );
                    */
                    /*
                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.DirectHaul.StatusOverlays.Enable.Label".Translate(),
                        description: "PressR.Settings.DirectHaul.StatusOverlays.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableStatusOverlays,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableStatusOverlays = newValue,
                        indentLevel: nextIndentLevel
                    );
                    */
                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.DirectHaul.RadiusIndicator.Enable.Label".Translate(),
                        description: "PressR.Settings.DirectHaul.RadiusIndicator.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableRadiusIndicator,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableRadiusIndicator = newValue,
                        indentLevel: nextIndentLevel
                    );
                    /*
                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.DirectHaul.StorageCreationPreview.Enable.Label".Translate(),
                        description: "PressR.Settings.DirectHaul.StorageCreationPreview.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableStorageCreationPreview,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableStorageCreationPreview = newValue,
                        indentLevel: nextIndentLevel
                    );
                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.DirectHaul.StorageHighlight.Enable.Label".Translate(),
                        description: "PressR.Settings.DirectHaul.StorageHighlight.Enable.Description".Translate(),
                        currentValue: settings.directHaulSettings.enableStorageHighlightOnHover,
                        onValueChanged: (newValue) =>
                            settings.directHaulSettings.enableStorageHighlightOnHover = newValue,
                        indentLevel: nextIndentLevel
                    );
                    */
                    SettingsItem.Draw(
                        sectionListing,
                        label: "PressR.Settings.DirectHaul.InvertKeys.Enable.Label".Translate(),
                        description: "PressR.Settings.DirectHaul.InvertKeys.Enable.Description".Translate(),
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
