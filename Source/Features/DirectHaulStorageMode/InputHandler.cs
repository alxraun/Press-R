using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Microtools.Features.DirectHaulStorageMode
{
    public class InputHandler(State state, ActionDispatcher action)
    {
        private readonly State _state = state;
        private readonly ActionDispatcher _action = action;

        public void HandleClick(IntVec3 clickCell)
        {
            var storage = _action.FindStorageAt(clickCell);
            if (storage == null)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            if (_state.SelectedThings.Count > 0)
            {
                var defsToToggle = _state.SelectedThings.Select(t => t.def);
                _action.ToggleThingDefsAllowance(storage, defsToToggle);
            }
            else
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
        }

        public void HandleDrag(IntVec3 startCell, IntVec3 endCell)
        {
            var allowedRect = _state.CurrentMap.BoundsRect(GenGrid.NoZoneEdgeWidth);
            var rect = CellRect.FromLimits(startCell, endCell).ClipInsideRect(allowedRect);
            if (rect.IsEmpty)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            var startZone = _action.FindStockpileAt(startCell);
            var changed = false;

            if (startZone != null)
            {
                if (_action.ExpandStockpile(startZone, rect))
                {
                    changed = true;
                }
            }
            else
            {
                var newZone = _action.CreateStockpileAt(rect);
                if (newZone != null)
                {
                    startZone = newZone;
                    changed = true;
                }
            }

            if (changed)
            {
                SoundDefOf.Designate_ZoneAdd_Stockpile.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
            }
        }
    }
}
