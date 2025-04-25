using System;
using UnityEngine;
using Verse;

namespace PressR.UI.Components
{
    public static class ActionButton
    {
        public static bool Draw(
            Rect rect,
            string label,
            Action onClick,
            string tooltip = null,
            bool disabled = false
        )
        {
            if (onClick == null || label == null)
            {
                Widgets.ButtonText(rect, label ?? "ERROR", active: false);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    TooltipHandler.TipRegion(rect, tooltip);
                }
                return false;
            }

            bool clicked = false;
            Color originalColor = GUI.color;

            if (disabled)
            {
                GUI.color = Widgets.InactiveColor;
            }

            if (Widgets.ButtonText(rect, label, active: !disabled))
            {
                if (!disabled)
                {
                    onClick.Invoke();
                    clicked = true;
                }
            }

            GUI.color = originalColor;

            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return clicked;
        }
    }
}
