using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static PressR.UI.Components.Constants;

namespace PressR.UI.Components
{
    public static class SettingsItem
    {
        public static void Draw(
            Listing_Standard listing,
            bool currentValue,
            string label = null,
            string description = null,
            string tooltip = null,
            bool disabled = false,
            float checkboxWidth = CheckboxSize,
            float rightMargin = CheckboxRightMargin,
            int indentLevel = 0,
            Action<bool> onValueChanged = null
        )
        {
            if (listing == null)
                return;

            var (totalHeight, labelHeight, descHeight) = CalculateDimensions(
                listing,
                description,
                indentLevel,
                checkboxWidth,
                rightMargin
            );
            Rect itemRect = listing.GetRect(totalHeight);

            float availableWidth = itemRect.width - checkboxWidth - rightMargin;
            Rect contentRect = new Rect(itemRect.x, itemRect.y, availableWidth, itemRect.height);
            Rect checkboxRect = CalculateCheckboxRect(itemRect, checkboxWidth, rightMargin);

            DrawLabelAndDescription(contentRect, label, description, labelHeight, descHeight);
            DrawCheckbox(checkboxRect, currentValue, disabled);
            HandleInteraction(itemRect, currentValue, disabled, onValueChanged);
            DrawTooltipAndMouseover(itemRect, tooltip, disabled);

            listing.Gap(listing.verticalSpacing);
        }

        private static (float totalHeight, float labelHeight, float descHeight) CalculateDimensions(
            Listing_Standard listing,
            string description,
            int indentLevel,
            float checkboxWidth,
            float rightMargin
        )
        {
            float labelHeight = Text.LineHeight;
            float descHeight = 0f;
            bool descriptionProvided = !string.IsNullOrEmpty(description);

            if (descriptionProvided)
            {
                float indentOffset = indentLevel * ItemPadding;
                float availableWidthForDesc =
                    listing.ColumnWidth
                    - indentOffset
                    - checkboxWidth
                    - rightMargin
                    - (TextHorizontalPadding * 2);
                descHeight = Text.CalcHeight(description, availableWidthForDesc);
            }

            float verticalGap = descriptionProvided ? DescriptionGap : 0f;
            float totalHeight = labelHeight + verticalGap + descHeight;
            return (totalHeight, labelHeight, descHeight);
        }

        private static Rect CalculateCheckboxRect(
            Rect itemRect,
            float checkboxWidth,
            float rightMargin
        )
        {
            return new Rect(
                itemRect.xMax - rightMargin - checkboxWidth,
                itemRect.y + (itemRect.height - CheckboxSize) / 2f,
                CheckboxSize,
                CheckboxSize
            );
        }

        private static void DrawLabelAndDescription(
            Rect contentRect,
            string label,
            string description,
            float labelHeight,
            float descHeight
        )
        {
            Rect labelRect = new Rect(
                contentRect.x + TextHorizontalPadding,
                contentRect.y,
                contentRect.width - TextHorizontalPadding * 2,
                labelHeight
            );
            Widgets.Label(labelRect, label ?? string.Empty);

            if (descHeight > 0)
            {
                Rect descRect = new Rect(
                    contentRect.x + TextHorizontalPadding,
                    labelRect.yMax + DescriptionGap,
                    contentRect.width - TextHorizontalPadding * 2,
                    descHeight
                );
                using (new TextBlock(GameFont.Tiny, Color.gray))
                {
                    Widgets.Label(descRect, description ?? string.Empty);
                }
            }
        }

        private static void DrawCheckbox(Rect checkboxRect, bool currentValue, bool disabled)
        {
            bool stateForDrawing = currentValue;
            Widgets.Checkbox(checkboxRect.position, ref stateForDrawing, CheckboxSize, disabled);
        }

        private static void HandleInteraction(
            Rect itemRect,
            bool currentValue,
            bool disabled,
            Action<bool> onValueChanged
        )
        {
            if (!disabled && Widgets.ButtonInvisible(itemRect))
            {
                bool newValue = !currentValue;
                onValueChanged?.Invoke(newValue);
                PlayToggleSound(newValue);
            }
        }

        private static void PlayToggleSound(bool state)
        {
            if (state)
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            else
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
        }

        private static void DrawTooltipAndMouseover(Rect itemRect, string tooltip, bool disabled)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(itemRect, tooltip);
            }
            if (!disabled)
            {
                Widgets.DrawHighlightIfMouseover(itemRect);
            }
        }
    }
}
