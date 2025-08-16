using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaulStorageMode
{
    public class ActionDispatcher(State state)
    {
        private readonly State _state = state;

        public IStoreSettingsParent FindStorageAt(IntVec3 cell)
        {
            var map = _state.CurrentMap;
            if (map == null || !cell.InBounds(map))
            {
                return null;
            }

            var parent = map.haulDestinationManager.SlotGroupParentAt(cell) as IStoreSettingsParent;
            return parent ?? (cell.GetZone(map) as IStoreSettingsParent);
        }

        public Zone_Stockpile FindStockpileAt(IntVec3 cell)
        {
            var map = _state.CurrentMap;
            if (map == null || !cell.InBounds(map))
            {
                return null;
            }
            return cell.GetZone(map) as Zone_Stockpile;
        }

        public Zone_Stockpile CreateStockpileAt(CellRect rect)
        {
            var map = _state.CurrentMap;
            if (map == null)
            {
                return null;
            }

            var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
            map.zoneManager.RegisterZone(zone);
            zone.settings.filter.SetDisallowAll();

            var changed = false;
            var allowedRect = map.BoundsRect(GenGrid.NoZoneEdgeWidth);
            var clipped = rect.ClipInsideRect(allowedRect);
            foreach (var cell in clipped.Cells)
            {
                if (CanPlaceStockpileAt(cell))
                {
                    zone.AddCell(cell);
                    changed = true;
                }
            }

            if (!changed)
            {
                map.zoneManager.DeregisterZone(zone);
                return null;
            }

            zone.slotGroup.RemoveHaulDesignationOnStoredThings();

            return zone;
        }

        public bool ExpandStockpile(Zone_Stockpile zone, CellRect expansionRect)
        {
            var map = _state.CurrentMap;
            var changed = false;
            var clipped = expansionRect.ClipInsideRect(map.BoundsRect(GenGrid.NoZoneEdgeWidth));
            foreach (var cell in clipped.Cells)
            {
                if (!zone.ContainsCell(cell) && CanPlaceStockpileAt(cell))
                {
                    zone.AddCell(cell);
                    changed = true;
                }
            }
            if (changed)
            {
                zone.slotGroup.RemoveHaulDesignationOnStoredThings();
            }
            return changed;
        }

        private bool CanPlaceStockpileAt(IntVec3 cell)
        {
            var map = _state.CurrentMap;
            if (map.zoneManager.ZoneAt(cell) != null)
                return false;

            if (!Designator_ZoneAdd.IsZoneableCell(cell, map).Accepted)
                return false;

            if (cell.GetTerrain(map).passability == Traversability.Impassable)
                return false;

            return true;
        }

        public bool ToggleThingDefsAllowance(
            IStoreSettingsParent storage,
            IEnumerable<ThingDef> defsToToggle,
            bool? forceAllow = null
        )
        {
            var settings = storage.GetStoreSettings();
            var parentSettings = storage.GetParentStoreSettings();
            if (settings == null || parentSettings == null)
            {
                return false;
            }

            var thingDefs = defsToToggle as ThingDef[] ?? defsToToggle.ToArray();
            if (!thingDefs.Any())
            {
                return false;
            }

            var allow = forceAllow ?? !thingDefs.All(d => settings.filter.Allows(d));

            var changed = false;
            foreach (var def in thingDefs)
            {
                if (!parentSettings.filter.Allows(def))
                {
                    continue;
                }
                if (settings.filter.Allows(def) != allow)
                {
                    settings.filter.SetAllow(def, allow);
                    changed = true;
                }
            }

            if (changed)
            {
                (
                    allow ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff
                ).PlayOneShotOnCamera();
            }

            return changed;
        }
    }
}
