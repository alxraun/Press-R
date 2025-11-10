using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Microtools.Features.DirectHaul
{
    public class JobDriver_DirectHaul : JobDriver
    {
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;

        private DirectHaul DirectHaul => pawn.Map?.GetMicrotoolsMapComponent()?.DirectHaul;
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
            this.FailOn(() => !StoreCell.InAllowedArea(pawn));
            this.FailOn(() => !HaulableThing.Position.InAllowedArea(pawn));

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
            var toil = ToilMaker.MakeToil("StartCarryDirectHaul");
            toil.initAction = StartCarryAction;
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private void StartCarryAction()
        {
            var originalTargetThing = HaulableThing;
            if (originalTargetThing == null)
            {
                LogIncompletable($"Target thing {HaulableInd} is null.");
                return;
            }

            var availableStackSpace = pawn.carryTracker.AvailableStackSpace(
                originalTargetThing.def
            );
            var desiredCount = job.count > 0 ? job.count : int.MaxValue;
            var numToCarry = Math.Min(
                Math.Min(desiredCount, availableStackSpace),
                originalTargetThing.stackCount
            );

            if (numToCarry <= 0)
            {
                LogIncompletable("Cannot carry any items (numToCarry <= 0).");
                return;
            }

            var carriedCount = pawn.carryTracker.TryStartCarry(originalTargetThing, numToCarry);

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
            {
                return;
            }

            var directHaul = DirectHaul;
            if (directHaul == null)
            {
                return;
            }

            var targetCell = directHaul.ThingStateManager.GetTarget(originalThing);
            if (targetCell.IsValid)
            {
                directHaul.ThingStateManager.SetPending(carriedThing, targetCell);
            }
        }

        private Toil JumpIfCannotCarry() =>
            Toils_Jump.JumpIf(GoToHaulable(), () => pawn.carryTracker.CarriedThing == null);

        private Toil GoToStoreCell() =>
            Toils_Goto
                .GotoCell(StoreCellInd, PathEndMode.ClosestTouch)
                .FailOn(() => !StoreCell.InAllowedArea(pawn));

        private Toil PlaceItem()
        {
            var toil = ToilMaker.MakeToil("PlaceDirectHauledThing");
            toil.initAction = PlaceHauledThingAction;
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private void PlaceHauledThingAction()
        {
            var carriedThing = pawn.carryTracker.CarriedThing;
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

            var dropResult = pawn.carryTracker.TryDropCarriedThing(
                cell,
                ThingPlaceMode.Direct,
                out var resultingThing
            );

            if (dropResult)
            {
                HandleSuccessfulDrop(resultingThing ?? carriedThing);
                return true;
            }
            return false;
        }

        private bool TryPlaceThingInAdjacentCell(Thing carriedThing, IntVec3 primaryTargetCell)
        {
            foreach (var offset in GenAdj.AdjacentCells8WayRandomized())
            {
                var adjacentCell = primaryTargetCell + offset;
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
            var map = pawn.Map;
            if (!cell.InBounds(map) || !cell.Standable(map) || cell.Impassable(map))
            {
                return false;
            }
            if (!cell.InAllowedArea(pawn))
            {
                return false;
            }

            var currentStackCountInCell = 0;
            var thingsInCell = cell.GetThingList(map);

            foreach (var th in thingsInCell)
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

            return true;
        }

        private void HandleSuccessfulDrop(Thing droppedThing)
        {
            var directHaul = DirectHaul;
            if (directHaul != null && droppedThing != null)
            {
                directHaul.ThingStateManager.SetHeld(droppedThing);
            }
        }

        private void FailAndCancelPlacement(Thing carriedThing, IntVec3 primaryTargetCell)
        {
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            var directHaul = DirectHaul;
            if (directHaul == null)
                return;
            if (carriedThing != null && !carriedThing.Destroyed)
            {
                directHaul.ThingStateManager.Remove(carriedThing);
            }
        }

        private void LogIncompletable(string reason)
        {
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
        }
    }
}
