using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.UI;

namespace PressR.Utils
{
    public static class InputUtils
    {
        public static IntVec3 GetMouseMapCell()
        {
            Map currentMap = Find.CurrentMap;
            IntVec3 cell = MouseCell();
            return (currentMap != null && cell.InBounds(currentMap)) ? cell : IntVec3.Invalid;
        }

        public static Thing GetInteractableThingUnderMouse(
            Map map,
            Predicate<Thing> isRelevantFilter
        )
        {
            if (map == null || isRelevantFilter == null)
                return null;

            List<Thing> thingsUnderMouse = GetRawThingsUnderMouse(map);

            foreach (Thing thing in thingsUnderMouse)
            {
                if (thing is Pawn pawn)
                {
                    Thing carriedThing = pawn.carryTracker?.CarriedThing;
                    if (carriedThing != null && isRelevantFilter(carriedThing))
                    {
                        return carriedThing;
                    }

                    if (isRelevantFilter(pawn))
                    {
                        return pawn;
                    }
                    continue;
                }

                if (isRelevantFilter(thing))
                {
                    return thing;
                }
            }

            return null;
        }

        private static List<Thing> GetRawThingsUnderMouse(Map map)
        {
            if (map == null)
                return new List<Thing>();

            Vector3 mouseMapPos = MouseMapPosition();

            TargetingParameters clickParams = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetItems = true,
                canTargetBuildings = false,
                mustBeSelectable = false,
                mapObjectTargetsMustBeAutoAttackable = false,
            };

            return GenUI.ThingsUnderMouse(mouseMapPos, 1f, clickParams);
        }
    }
}
