using System.Collections.Generic;
using UnityEngine;
using Verse;
using static PressR.Debugger.DebuggerTrackedValueInfo;

namespace PressR.Debugger.Configs
{
    /*
    This is an example configuration file for the debugger.
    Implement the IDebuggerConfig interface and define the GetTrackedValues method
    to specify which game data you want to monitor.
    */
    public class ExampleConfig : IDebuggerConfig
    {
        /* User-friendly name displayed in the debugger window's config dropdown. */
        public string Name => "Example Config";

        /* How often (in seconds) the debugger updates the displayed values while recording. Lower values mean more frequent updates. */
        public float UpdateInterval => 0.5f;

        /* Delay (in seconds) after pressing 'Start Recording' before data capture actually begins. */
        public float StartDelaySeconds => 1.0f;

        /* This method returns the list of values the debugger should track and display. */
        public IEnumerable<DebuggerTrackedValueInfo> GetTrackedValues()
        {
            return new List<DebuggerTrackedValueInfo>
            {
                /* TrackValue: Tracks a single value. The debugger displays the result of value.ToString(). */
                TrackValue("Find.CurrentMap.Biome.label", "Current Biome Label"),
                /* TrackValue: Handles potentially null values gracefully, displaying "null". */
                TrackValue("Find.Selector.SingleSelectedThing", "Selected Thing (if any)"),
                /* TrackValue: Can access static properties/fields as well. */
                TrackValue("RimWorld.Faction.OfPlayer.def.label", "Player Faction Def Label"),
                /* TrackCollectionCount: Tracks ONLY the number of items in a collection (like List<T>). */
                TrackCollectionCount("Find.CurrentMap.mapPawns.AllPawns", "Map Pawns Count"),
                /* TrackCollectionCount: Also works for dictionaries (IDictionary), tracking key-value pairs count. Find.World.worldObjects.AllWorldObjects is a List<WorldObject>. */
                TrackCollectionCount(
                    "Find.World.worldObjects.AllWorldObjects",
                    "World Objects Count"
                ),
                /* TrackCollectionItems: Tracks items within a collection. Use .ShowAll() to display every item. */
                TrackCollectionItems(
                        "Find.Selector.SelectedObjectsListForReading",
                        "Selected Items (All)"
                    )
                    .ShowAll(),
                /* TrackCollectionItems: Use .WithLimit(N) to show only the first N items. (Note: Method calls in paths like 'ThingsOfDef()' are not supported) */
                TrackCollectionItems(
                        "Find.CurrentMap.listerThings.AllThings",
                        "All Things on Map (<=5)"
                    )
                    .WithLimit(5),
                /* TrackCollectionItems: Without .ShowAll() or .WithLimit(N), shows a default number of items (e.g., 3). */
                TrackCollectionItems(
                    "ThingDefOf.MealSimple.comps",
                    "Simple Meal Comps (Default Limit)"
                ),
                /* TrackCollectionItems: Can be used to track list members directly. */
                TrackCollectionItems(
                        "Find.FactionManager.AllFactionsListForReading",
                        "All Factions (<=5)"
                    )
                    .WithLimit(5),
                /* TrackDictionaryKeys: Tracks ONLY the keys of a dictionary. Use .ShowAll() to display all keys. */
                TrackDictionaryKeys(
                        "Find.CurrentMap.pawnDestinationReservationManager.reservedDestinations",
                        "Reserved Destinations Keys (All)"
                    )
                    .ShowAll(),
                /* Tracking list members with the default limit. */
                TrackCollectionItems(
                    "Find.FactionManager.AllFactionsListForReading",
                    "All Factions (Default Limit)"
                ),
                /* Tracking all active game conditions. Note: There's no direct public list for *only* permanent conditions. */
                TrackCollectionItems(
                        "Find.CurrentMap.gameConditionManager.ActiveConditions",
                        "Active Map Conditions (<=5)"
                    )
                    .WithLimit(5),
            };
        }
    }
}
