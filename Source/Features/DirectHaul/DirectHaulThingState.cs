using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using Verse;

namespace PressR.Features.DirectHaul
{
    public class DirectHaulThingState
    {
        private readonly DirectHaulFrameData _frameData;

        public DirectHaulThingState(DirectHaulFrameData frameData)
        {
            _frameData = frameData ?? throw new ArgumentNullException(nameof(frameData));
        }

        public List<Thing> MarkThingsAsPending(
            IReadOnlyList<IntVec3> cells,
            bool isHighPriority = false
        )
        {
            var directHaulData = _frameData?.ExposedData;
            var thingsToMark = _frameData?.NonPendingSelectedThings;

            if (
                directHaulData == null
                || thingsToMark == null
                || !thingsToMark.Any()
                || cells == null
                || cells.Count < thingsToMark.Count
            )
            {
                return [];
            }

            var successfullyMarked = new List<Thing>(thingsToMark.Count);
            for (int i = 0; i < thingsToMark.Count; i++)
            {
                Thing thing = thingsToMark[i];
                IntVec3 cell = cells[i];

                if (TryMarkSingleThingAsPending(thing, cell, directHaulData, isHighPriority))
                {
                    successfullyMarked.Add(thing);
                }
            }
            return successfullyMarked;
        }

        public int RemoveNonPendingSelectedThingsFromTracking()
        {
            var directHaulData = _frameData?.ExposedData;
            var thingsToRemove = _frameData?.NonPendingSelectedThings;

            if (directHaulData == null || thingsToRemove == null || !thingsToRemove.Any())
            {
                return 0;
            }

            return thingsToRemove
                .ToList()
                .Count(thing => TryRemoveSingleThingFromTracking(thing, directHaulData));
        }

        private static bool TryMarkSingleThingAsPending(
            Thing thing,
            IntVec3 cell,
            DirectHaulExposableData directHaulData,
            bool isHighPriority
        )
        {
            if (thing == null || !cell.IsValid || directHaulData == null)
            {
                return false;
            }

            directHaulData.MarkThingAsPending(thing, cell, isHighPriority);
            return true;
        }

        private static bool TryRemoveSingleThingFromTracking(
            Thing thing,
            DirectHaulExposableData directHaulData
        )
        {
            if (thing == null || directHaulData == null)
            {
                return false;
            }

            directHaulData.RemoveThingFromTracking(thing);
            return true;
        }
    }
}
