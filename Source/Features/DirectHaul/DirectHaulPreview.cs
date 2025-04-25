using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using Verse;

namespace PressR.Features.DirectHaul
{
    public sealed class DirectHaulPreview
    {
        public bool TryGetPreviewPositions(
            IntVec3 focus1,
            IntVec3 focus2,
            Map map,
            DirectHaulFrameData frameData,
            out Dictionary<Thing, IntVec3> previewPositions
        )
        {
            previewPositions = null;

            if (!IsValidContextForPreview(focus1, focus2, map, frameData))
            {
                return false;
            }

            IReadOnlyList<IntVec3> placementCells = frameData.CalculatedPlacementCells;
            IReadOnlyList<Thing> thingsToPlace = frameData.NonPendingSelectedThings;

            if (!AreEnoughPlacementCellsAvailable(placementCells, thingsToPlace))
            {
                return false;
            }

            previewPositions = MapThingsToPlacementCells(thingsToPlace, placementCells);
            return previewPositions.Count > 0;
        }

        private static bool IsValidContextForPreview(
            IntVec3 focus1,
            IntVec3 focus2,
            Map map,
            DirectHaulFrameData frameData
        )
        {
            return map != null
                && frameData != null
                && frameData.HasAnyNonPendingSelected
                && IsValidCellForPreview(focus1, map)
                && IsValidCellForPreview(focus2, map);
        }

        private static bool IsValidCellForPreview(IntVec3 cell, Map map)
        {
            return cell.InBounds(map) && !cell.Impassable(map);
        }

        private static bool AreEnoughPlacementCellsAvailable(
            IReadOnlyList<IntVec3> placementCells,
            IReadOnlyList<Thing> thingsToPlace
        )
        {
            return placementCells != null
                && thingsToPlace != null
                && placementCells.Count >= thingsToPlace.Count;
        }

        private static Dictionary<Thing, IntVec3> MapThingsToPlacementCells(
            IReadOnlyList<Thing> things,
            IReadOnlyList<IntVec3> cells
        )
        {
            if (things == null || cells == null)
            {
                return new Dictionary<Thing, IntVec3>();
            }

            return things
                .Zip(cells, (thing, cell) => new { Thing = thing, Cell = cell })
                .Where(pair => pair.Thing != null)
                .ToDictionary(pair => pair.Thing, pair => pair.Cell);
        }
    }
}
