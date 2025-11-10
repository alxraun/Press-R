using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Microtools.Features.DirectHaul
{
    public class WorkGiver_DirectHaul : WorkGiver_Scanner
    {
        private DirectHaul DirectHaul(Pawn pawn) =>
            pawn.Map?.GetMicrotoolsMapComponent()?.DirectHaul;

        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var directHaul = DirectHaul(pawn);
            if (directHaul == null)
            {
                return Enumerable.Empty<Thing>();
            }

            return directHaul.ThingStateManager.AllPendingThings;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var directHaul = DirectHaul(pawn);
            if (directHaul == null)
            {
                return null;
            }

            var targetCellInfo = directHaul.ThingStateManager.GetTarget(t);
            if (!targetCellInfo.IsValid)
            {
                return null;
            }

            if (!t.Position.InAllowedArea(pawn))
            {
                return null;
            }
            if (!targetCellInfo.Cell.InAllowedArea(pawn))
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

            var job = JobMaker.MakeJob(
                MicrotoolsDefOf.Microtools_DirectHaul,
                t,
                targetCellInfo.Cell
            );
            job.count = t.stackCount;
            job.playerForced = forced;
            job.haulMode = HaulMode.ToCellNonStorage;

            return job;
        }
    }
}
