using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul
{
    public class DirectHaulStorage
    {
        public IStoreSettingsParent FindStorageAt(IntVec3 cell)
        {
            Map map = Find.CurrentMap;
            if (map == null || !cell.InBounds(map))
                return null;

            Building building = GridsUtility.GetFirstBuilding(cell, map);
            if (building is Building_Storage buildingStorage)
                return buildingStorage;

            if (map.zoneManager.ZoneAt(cell) is Zone_Stockpile stockpileZone)
                return stockpileZone;

            return null;
        }

        public Zone_Stockpile FindStockpileAt(IntVec3 cell)
        {
            return FindStorageAt(cell) as Zone_Stockpile;
        }

        public Zone_Stockpile CreateStockpileZone(CellRect rect)
        {
            Map map = Find.CurrentMap;
            if (map == null || rect.IsEmpty)
                return null;

            var clipped = rect.ClipInsideMap(map);
            if (clipped.IsEmpty)
                return null;

            var stockpile = new Zone_Stockpile(
                StorageSettingsPreset.DefaultStockpile,
                map.zoneManager
            );
            map.zoneManager.RegisterZone(stockpile);
            SoundDefOf.Designate_ZoneAdd_Stockpile.PlayOneShotOnCamera();

            foreach (var cell in clipped.Cells)
            {
                if (CanPlaceZoneAt(cell))
                    stockpile.AddCell(cell);
            }
            return stockpile;
        }

        public Zone_Stockpile GetOrCreateStockpileZone(CellRect rect)
        {
            var center = rect.CenterCell;
            var existing = FindStockpileAt(center);
            if (existing != null)
                return existing;
            return CreateStockpileZone(rect);
        }

        public void ToggleThingDefsAllowance(
            IStoreSettingsParent storage,
            IEnumerable<ThingDef> defsToToggle
        )
        {
            if (storage is null || defsToToggle is null)
                return;

            StorageSettings parentSettings = storage.GetParentStoreSettings();
            StorageSettings currentSettings = storage.GetStoreSettings();

            if (parentSettings is null || currentSettings is null)
                return;

            var validDefsToToggle = defsToToggle.Where(d => d != null).Distinct().ToList();
            if (!validDefsToToggle.Any())
                return;

            bool allDefsFundamentallyAllowed = validDefsToToggle.All(def =>
                parentSettings.filter.Allows(def)
            );

            if (!allDefsFundamentallyAllowed)
            {
                return;
            }

            bool allCurrentlyAllowed = validDefsToToggle.All(def =>
                currentSettings.filter.Allows(def)
            );
            bool actionIsAllow = !allCurrentlyAllowed;

            bool changed = false;
            foreach (var def in validDefsToToggle)
            {
                if (currentSettings.filter.Allows(def) != actionIsAllow)
                {
                    currentSettings.filter.SetAllow(def, actionIsAllow);
                    changed = true;
                }
            }

            if (changed)
            {
                (
                    actionIsAllow ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff
                ).PlayOneShotOnCamera();
            }
        }

        public Zone_Stockpile GetOrCreateStockpileForAction(IntVec3 startCell, IntVec3 endCell)
        {
            var startZone = FindStockpileAt(startCell);
            var endZone = FindStockpileAt(endCell);

            if (startZone == null && endZone != null)
            {
                return null;
            }

            var rect = CellRect.FromLimits(startCell, endCell).ClipInsideMap(Find.CurrentMap);
            if (rect.IsEmpty)
                return null;
            Zone_Stockpile targetZone;

            if (startZone != null)
            {
                targetZone = startZone;
                ExpandStockpileZone(targetZone, rect);
            }
            else if (endZone != null)
            {
                targetZone = endZone;
                ExpandStockpileZone(targetZone, rect);
            }
            else
            {
                targetZone = CreateStockpileZone(rect);
                if (targetZone != null)
                {
                    targetZone.settings.filter.SetDisallowAll();
                }
            }

            return targetZone;
        }

        public void ExpandStockpileZone(Zone_Stockpile zone, CellRect expansionRect)
        {
            Map map = Find.CurrentMap;
            if (map == null || zone == null || expansionRect.IsEmpty)
                return;

            bool zoneExpanded = false;
            var clipped = expansionRect.ClipInsideMap(map);
            foreach (var cell in clipped.Cells)
            {
                if (CanPlaceZoneAt(cell) && !zone.ContainsCell(cell))
                {
                    zone.AddCell(cell);
                    zoneExpanded = true;
                }
            }

            if (zoneExpanded)
            {
                SoundDefOf.Designate_ZoneAdd_Stockpile.PlayOneShotOnCamera();
            }
        }

        private bool CanPlaceZoneAt(IntVec3 cell)
        {
            Map map = Find.CurrentMap;
            if (map == null || !cell.InBounds(map))
                return false;

            if (map.zoneManager.ZoneAt(cell) != null)
                return false;

            if (cell.GetTerrain(map).passability == Traversability.Impassable)
                return false;

            List<Thing> thingList = map.thingGrid.ThingsListAt(cell);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (!thingList[i].def.CanOverlapZones)
                    return false;
            }

            return true;
        }
    }
}
