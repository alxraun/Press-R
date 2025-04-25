using System;
using System.Collections.Generic;
using PressR.Features.DirectHaul.Core;
using Verse;
using Verse.AI;

namespace PressR.Features.DirectHaul.JobDrivers
{
    public class JobDriver_DirectHaul : JobDriver
    {
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;

        private DirectHaulExposableData DirectHaulData =>
            pawn.Map?.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
        private Thing HaulableThing => job.GetTarget(HaulableInd).Thing;
        private IntVec3 StoreCell => job.GetTarget(StoreCellInd).Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(HaulableThing, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(StoreCell, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(HaulableInd);
            this.FailOnForbidden(HaulableInd);

            yield return Toils_Reserve.Reserve(HaulableInd);
            yield return GoToHaulable();
            yield return CarryItem();
            yield return JumpIfCannotCarry();
            yield return Toils_Reserve.Reserve(StoreCellInd);
            yield return GoToStoreCell();
            yield return PlaceItem();
            yield return Toils_Reserve.Release(StoreCellInd);
            yield return Toils_Reserve.Release(HaulableInd);
        }

        private Toil GoToHaulable() =>
            Toils_Goto
                .GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(HaulableInd);

        private Toil CarryItem()
        {
            Toil toil = ToilMaker.MakeToil("StartCarryDirectHaul");
            toil.initAction = StartCarryAction;
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private void StartCarryAction()
        {
            Thing originalTargetThing = HaulableThing;
            if (originalTargetThing == null)
            {
                LogIncompletable($"Target thing {HaulableInd} is null.");
                return;
            }

            int availableStackSpace = pawn.carryTracker.AvailableStackSpace(
                originalTargetThing.def
            );
            int desiredCount = job.count > 0 ? job.count : int.MaxValue;
            int numToCarry = Math.Min(
                Math.Min(desiredCount, availableStackSpace),
                originalTargetThing.stackCount
            );

            if (numToCarry <= 0)
            {
                LogIncompletable("Cannot carry any items (numToCarry <= 0).");
                return;
            }

            int carriedCount = pawn.carryTracker.TryStartCarry(originalTargetThing, numToCarry);

            if (carriedCount <= 0)
            {
                LogIncompletable("Failed TryStartCarry.");
                return;
            }

            TransferPendingStatusIfSplit(originalTargetThing, pawn.carryTracker.CarriedThing);
        }

        private void TransferPendingStatusIfSplit(Thing originalThing, Thing carriedThing)
        {
            if (carriedThing == null || ReferenceEquals(carriedThing, originalThing))
                return;

            DirectHaulExposableData data = DirectHaulData;
            if (data == null)
                return;

            if (
                data.TryGetInfoFromPending(
                    originalThing,
                    out var targetCell,
                    out var isHighPriority
                )
            )
            {
                data.MarkThingAsPending(carriedThing, targetCell, isHighPriority);
            }
        }

        private Toil JumpIfCannotCarry() =>
            Toils_Jump.JumpIf(GoToHaulable(), () => pawn.carryTracker.CarriedThing == null);

        private Toil GoToStoreCell() => Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

        private Toil PlaceItem()
        {
            Toil toil = ToilMaker.MakeToil("PlaceDirectHauledThing");
            toil.initAction = PlaceHauledThingAction;
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private void PlaceHauledThingAction()
        {
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (!ValidatePlacementPreconditions(carriedThing, StoreCell))
                return;

            if (TryPlaceThingInCell(carriedThing, StoreCell))
                return;

            if (TryPlaceThingInAdjacentCell(carriedThing, StoreCell))
                return;

            FailAndCancelPlacement(carriedThing, StoreCell);
        }

        private bool ValidatePlacementPreconditions(Thing carriedThing, IntVec3 primaryTargetCell)
        {
            if (carriedThing == null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return false;
            }

            if (!primaryTargetCell.IsValid)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return false;
            }
            return true;
        }

        private bool TryPlaceThingInCell(Thing carriedThing, IntVec3 cell)
        {
            if (!CanPlaceInCell(cell, carriedThing))
            {
                return false;
            }

            if (pawn.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out Thing thing))
            {
                HandleSuccessfulDrop(cell, thing);
                return true;
            }
            return false;
        }

        private bool TryPlaceThingInAdjacentCell(Thing carriedThing, IntVec3 primaryTargetCell)
        {
            foreach (IntVec3 offset in GenAdj.AdjacentCells8WayRandomized())
            {
                IntVec3 adjacentCell = primaryTargetCell + offset;
                if (
                    CanPlaceInCell(adjacentCell, carriedThing)
                    && TryPlaceThingInCell(carriedThing, adjacentCell)
                )
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanPlaceInCell(IntVec3 cell, Thing thingToPlace)
        {
            Map map = pawn.Map;
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.Impassable(map))
            {
                return false;
            }

            int currentStackCountInCell = 0;
            List<Thing> thingsInCell = cell.GetThingList(map);

            foreach (Thing th in thingsInCell)
            {
                if (th.def.category == ThingCategory.Item)
                {
                    if (th.def != thingToPlace.def)
                        return false;
                    currentStackCountInCell += th.stackCount;
                }
                if (
                    th.def.entityDefToBuild != null
                    && th.def.entityDefToBuild.passability != Traversability.Standable
                )
                {
                    return false;
                }
                if (
                    th.def.passability != Traversability.Standable
                    && GenSpawn.SpawningWipes(thingToPlace.def, th.def)
                )
                {
                    return false;
                }
            }

            if (currentStackCountInCell + thingToPlace.stackCount > thingToPlace.def.stackLimit)
            {
                return false;
            }

            DirectHaulExposableData data = DirectHaulData;
            if (data != null && data.IsCellPendingTarget(cell, thingToPlace))
            {
                return false;
            }

            if (map.haulDestinationManager.SlotGroupAt(cell) != null)
            {
                return false;
            }

            return true;
        }

        private void HandleSuccessfulDrop(IntVec3 cell, Thing droppedThing)
        {
            DirectHaulExposableData data = DirectHaulData;
            if (data != null && droppedThing != null)
            {
                data.SetThingAsHeldAt(droppedThing, cell, job.playerForced);
            }
        }

        private void FailAndCancelPlacement(Thing carriedThing, IntVec3 primaryTargetCell)
        {
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            DirectHaulExposableData data = DirectHaulData;
            if (data != null)
            {
                data.RemoveThingFromTracking(carriedThing);
            }
        }

        private void LogIncompletable(string reason)
        {
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
        }
    }
}
