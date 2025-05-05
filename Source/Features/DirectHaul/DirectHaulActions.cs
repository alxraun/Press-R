using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul
{
    public static class DirectHaulActions
    {
        public static void ExecutePlacementDesignation(
            DirectHaulState state,
            DirectHaulPlacement placement,
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulMode mode
        )
        {
            if (!state.HasValidCalculatedPlacementCells(focus1, focus2, mode))
            {
                var calculatedPlacementCells = placement.FindPlacementCells(state);
                state.SetCalculatedPlacementCells(calculatedPlacementCells, focus1, focus2, mode);
            }

            var placementCells = state.CalculatedPlacementCells.ToList();

            if (placementCells.Count < state.NonPendingSelectedThings.Count)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            bool isHighPriority = mode == DirectHaulMode.HighPriority;
            var markedThings = state.MarkThingsPending(placementCells, isHighPriority);

            if (markedThings.Any())
            {
                SoundDefOf.Designate_Haul.PlayOneShotOnCamera();
                state.UpdatePendingStatusLocally(markedThings);
            }
            else
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
            }
        }

        public static void ExecuteCancelPlacementDesignation(DirectHaulState state)
        {
            int removedCount = state.RemoveThingsFromTracking(state.AllSelectedThings);

            if (removedCount > 0)
            {
                SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
            }
        }

        public static void ExecuteStockpileZoneCreationOrExpansion(
            DirectHaulState state,
            DirectHaulStorage storage,
            IntVec3 startCell,
            IntVec3 endCell
        )
        {
            Zone_Stockpile targetZone = storage.GetOrCreateStockpileForAction(startCell, endCell);

            if (targetZone == null)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }
        }

        public static void ExecuteStorageClickAction(
            DirectHaulState state,
            DirectHaulStorage storage,
            IStoreSettingsParent storageUnderClick
        )
        {
            if (storageUnderClick == null)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            if (state.HasAnyNonPendingSelected)
            {
                var defsToToggle = state.NonPendingSelectedThings.Select(t => t.def);
                storage.ToggleThingDefsAllowance(storageUnderClick, defsToToggle);
            }
            else
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
        }
    }
}
