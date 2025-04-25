using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul
{
    public class WorkGiver_DirectHaul : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            PressRMapComponent mapComponent = pawn.Map?.GetComponent<PressRMapComponent>();
            if (mapComponent?.DirectHaulExposableData == null)
            {
                return Enumerable.Empty<Thing>();
            }

            return mapComponent.DirectHaulExposableData.GetThingsWithStatus(
                DirectHaulStatus.Pending
            );
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            PressRMapComponent mapComponent = pawn.Map?.GetComponent<PressRMapComponent>();
            if (mapComponent?.DirectHaulExposableData == null)
                return null;

            if (
                mapComponent.DirectHaulExposableData.GetStatusForThing(t)
                != DirectHaulStatus.Pending
            )
            {
                return null;
            }

            if (
                !mapComponent.DirectHaulExposableData.TryGetInfoFromPending(
                    t,
                    out var targetCellInfo,
                    out bool isHighPriority
                )
            )
            {
                return null;
            }

            if (!pawn.CanReach(t, PathEndMode.ClosestTouch, MaxPathDanger(pawn)))
            {
                return null;
            }
            if (!pawn.CanReach(targetCellInfo.Cell, PathEndMode.ClosestTouch, MaxPathDanger(pawn)))
            {
                return null;
            }

            bool canPlaceInTargetCell = true;
            int currentStackCountInCell = 0;
            List<Thing> thingsInCell = targetCellInfo.Cell.GetThingList(pawn.Map);

            foreach (Thing th in thingsInCell)
            {
                if (th.def.category == ThingCategory.Item)
                {
                    if (th.def != t.def)
                    {
                        canPlaceInTargetCell = false;
                        JobFailReason.Is("DirectHaulTargetCellOccupiedByIncompatible".Translate());
                        break;
                    }

                    /*
                    if (mapComponent.DirectHaulExposableData.GetStatusForThing(th) != DirectHaulStatus.Held)
                    {
                        canPlaceInTargetCell = false;
                        JobFailReason.Is("DirectHaulTargetCellOccupiedByNonHeld".Translate());
                        break;
                    }
                    */


                    currentStackCountInCell += th.stackCount;
                }
            }

            if (canPlaceInTargetCell && currentStackCountInCell + t.stackCount > t.def.stackLimit)
            {
                canPlaceInTargetCell = false;
                JobFailReason.Is("DirectHaulTargetCellFull".Translate());
            }

            if (!canPlaceInTargetCell)
            {
                return null;
            }

            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                JobFailReason.Is("Reserved".Translate(t.LabelCap, t));
                return null;
            }
            if (!pawn.CanReserve(targetCellInfo.Cell, 1, -1, null, forced))
            {
                JobFailReason.Is(
                    "Reserved".Translate(
                        targetCellInfo.Thing?.LabelCap ?? "Cell".Translate(),
                        pawn.LabelShort
                    )
                );
                return null;
            }

            Job job = JobMaker.MakeJob(PressRDefOf.PressR_DirectHaul, t, targetCellInfo.Cell);
            job.count = t.stackCount;
            job.playerForced = isHighPriority;
            job.haulMode = HaulMode.ToCellNonStorage;

            return job;
        }
    }
}
