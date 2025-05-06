using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using static Verse.UI;

namespace PressR.Utils
{
    public static class MapUtils
    {
        public static List<T> GetThingsInCellOfType<T>(IntVec3 cell, Map map)
            where T : Thing
        {
            if (map == null || !cell.InBounds(map))
            {
                return new List<T>();
            }

            return map.thingGrid.ThingsListAtFast(cell).OfType<T>().ToList();
        }

        public static HashSet<T> GetVisibleThingsInViewRectOfType<T>(
            Map map,
            Predicate<T> filter = null
        )
            where T : Thing
        {
            if (map == null)
                return new HashSet<T>();

            HashSet<T> visibleThings = new HashSet<T>();
            CellRect viewRect = Find.CameraDriver.CurrentViewRect;

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing is T typedThing)
                {
                    IntVec3 position = typedThing.PositionHeld;
                    if (
                        position.IsValid
                        && viewRect.Contains(position)
                        && !map.fogGrid.IsFogged(position)
                        && (filter == null || filter(typedThing))
                    )
                    {
                        visibleThings.Add(typedThing);
                    }
                }
            }

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                IntVec3 pawnPosition = pawn.PositionHeld;

                if (
                    pawnPosition.IsValid
                    && viewRect.Contains(pawnPosition)
                    && !map.fogGrid.IsFogged(pawnPosition)
                )
                {
                    Thing carriedThing = pawn.carryTracker?.CarriedThing;
                    if (
                        carriedThing is T typedCarriedThing
                        && (filter == null || filter(typedCarriedThing))
                    )
                    {
                        visibleThings.Add(typedCarriedThing);
                    }
                }
            }

            return visibleThings;
        }

        public static bool IsThingInViewRect(Thing thing)
        {
            if (thing == null || !thing.SpawnedOrAnyParentSpawned)
                return false;

            return Find.CameraDriver.CurrentViewRect.Contains(thing.PositionHeld);
        }

        public static T GetThingOrZoneAtMouseCell<T>(Map map)
            where T : class
        {
            if (map == null || MouseCell().InBounds(map) == false)
            {
                return null;
            }

            IntVec3 mouseCell = MouseCell();

            var thing = map.thingGrid.ThingsListAtFast(mouseCell).OfType<T>().FirstOrDefault();

            if (thing != null)
            {
                return thing;
            }

            if (typeof(T).IsInterface || typeof(Zone).IsAssignableFrom(typeof(T)))
            {
                var zone = map.zoneManager.ZoneAt(mouseCell);
                if (zone is T zoneAsT)
                {
                    return zoneAsT;
                }
            }

            return null;
        }
    }
}
