using Microtools.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Microtools.UI.Components.Constants;

namespace Microtools.UI.Components
{
    public static class SettingsResetButton
    {
        private static readonly Color BackgroundColor = new(0.55f, 0.55f, 0.4f, 0.2f);

        public static void Draw(Listing_Standard listing, MicrotoolsSettings settings)
        {
            if (listing == null || settings == null)
                return;

            Rect buttonRect = listing.GetRect(ButtonHeight);

            DrawBackground(buttonRect);
            DrawLabel(buttonRect);
            HandleInteraction(buttonRect, settings);
            DrawTooltipAndMouseover(buttonRect);

            listing.Gap(listing.verticalSpacing);
        }

        private static void DrawBackground(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, BackgroundColor);
        }

        private static void DrawLabel(Rect rect)
        {
            var previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            using (new TextBlock(GameFont.Small))
            {
                Widgets.Label(rect, "Microtools.Settings.ResetButton.Label".Translate());
            }
            Text.Anchor = previousAnchor;
        }

        private static void HandleInteraction(Rect buttonRect, MicrotoolsSettings settings)
        {
            if (Widgets.ButtonInvisible(buttonRect))
            {
                settings.ResetToDefaults();
                Messages.Message(
                    "Microtools.Settings.ResetButton.Message".Translate(),
                    MessageTypeDefOf.PositiveEvent
                );
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private static void DrawTooltipAndMouseover(Rect rect)
        {
            Widgets.DrawHighlightIfMouseover(rect);
        }
    }
}
