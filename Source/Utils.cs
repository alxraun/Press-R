using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using static Verse.UI;

namespace PressR
{
    public static class Utils
    {
        public static void GetSelectedHaulableThings(List<Thing> result)
        {
            result.Clear();
            var selected = Find.Selector.SelectedObjectsListForReading;
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] is Thing thing && thing.def.EverHaulable)
                {
                    result.Add(thing);
                }
            }
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
                return [];

            Vector3 mouseMapPos = MouseMapPosition();

            TargetingParameters clickParams = new()
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
