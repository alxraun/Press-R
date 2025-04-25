using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static PressR.UI.Components.Constants;

namespace PressR.UI.Components
{
    public static class FeatureSection
    {
        public static void Draw(
            Listing_Standard listing,
            ref bool isEnabled,
            string label = null,
            string description = null,
            string tooltip = null,
            int currentIndentLevel = 0,
            bool centerTitleVertically = false,
            Action<Listing_Standard, int> drawContentAction = null
        )
        {
            if (listing == null)
                return;

            bool currentlyEnabled = DrawHeader(
                listing,
                label,
                description,
                ref isEnabled,
                tooltip,
                centerTitleVertically
            );

            if (currentlyEnabled)
            {
                listing.Indent(ItemPadding);
                drawContentAction?.Invoke(listing, currentIndentLevel + 1);
                listing.Outdent(ItemPadding);
            }
        }

        private static bool DrawHeader(
            Listing_Standard listing,
            string label,
            string description,
            ref bool isEnabled,
            string tooltip,
            bool centerTitleVertically
        )
        {
            float labelHeight = Text.LineHeightOf(GameFont.Medium);
            float descHeight = 0f;
            bool descriptionProvided = !string.IsNullOrEmpty(description);

            float availableWidthForText =
                listing.ColumnWidth
                - CheckboxSize
                - CheckboxRightMargin
                - (TextHorizontalPadding * 2);

            if (descriptionProvided)
            {
                descHeight = Text.CalcHeight(description, availableWidthForText);
            }
            else if (centerTitleVertically)
            {
                descHeight = Text.LineHeightOf(GameFont.Tiny);
            }

            float verticalGap = descriptionProvided ? DescriptionGap : 0f;
            float totalHeight = labelHeight + verticalGap + descHeight;
            Rect headerRect = listing.GetRect(totalHeight);

            DrawBackgroundAndHighlight(headerRect);

            Rect contentRect = new Rect(
                headerRect.x,
                headerRect.y,
                headerRect.width - CheckboxSize - CheckboxRightMargin,
                headerRect.height
            );
            Rect checkboxRect = CalculateCheckboxRect(headerRect);

            DrawLabelAndDescription(
                contentRect,
                label,
                description,
                labelHeight,
                descHeight,
                centerTitleVertically,
                descriptionProvided
            );
            DrawCheckbox(checkboxRect, isEnabled);
            HandleHeaderInteraction(headerRect, ref isEnabled);
            DrawTooltipAndMouseover(headerRect, tooltip);

            return isEnabled;
        }

        private static Rect CalculateCheckboxRect(Rect headerRect)
        {
            return new Rect(
                headerRect.xMax - CheckboxRightMargin - CheckboxSize,
                headerRect.y + (headerRect.height - CheckboxSize) / 2f,
                CheckboxSize,
                CheckboxSize
            );
        }

        private static void DrawLabelAndDescription(
            Rect contentRect,
            string label,
            string description,
            float labelHeight,
            float descHeight,
            bool centerTitleVertically,
            bool descriptionProvided
        )
        {
            float labelY = contentRect.y;
            if (centerTitleVertically && !descriptionProvided)
            {
                labelY = contentRect.y + (contentRect.height - labelHeight) / 2f;
            }

            Rect labelRect = new Rect(
                contentRect.x + TextHorizontalPadding,
                labelY,
                contentRect.width - TextHorizontalPadding * 2,
                labelHeight
            );

            using (new TextBlock(GameFont.Medium))
            {
                Widgets.Label(labelRect, label ?? string.Empty);
            }

            if (descriptionProvided)
            {
                float descriptionY = labelRect.yMax + DescriptionGap;
                Rect descRect = new Rect(
                    contentRect.x + TextHorizontalPadding,
                    descriptionY,
                    contentRect.width - TextHorizontalPadding * 2,
                    descHeight
                );
                using (new TextBlock(GameFont.Tiny, Color.gray))
                {
                    Widgets.Label(descRect, description);
                }
            }
        }

        private static void DrawCheckbox(Rect checkboxRect, bool isEnabled)
        {
            bool stateForDrawing = isEnabled;
            Widgets.Checkbox(checkboxRect.position, ref stateForDrawing, CheckboxSize);
        }

        private static void HandleHeaderInteraction(Rect headerRect, ref bool isEnabled)
        {
            if (Widgets.ButtonInvisible(headerRect))
            {
                isEnabled = !isEnabled;
                PlayToggleSound(isEnabled);
            }
        }

        private static void PlayToggleSound(bool state)
        {
            if (state)
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            else
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
        }

        private static void DrawBackgroundAndHighlight(Rect rect)
        {
            Widgets.DrawLightHighlight(rect);
        }

        private static void DrawTooltipAndMouseover(Rect rect, string tooltip)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            Widgets.DrawHighlightIfMouseover(rect);
        }
    }
}
