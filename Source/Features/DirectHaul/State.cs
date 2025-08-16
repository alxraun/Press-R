using System.Collections.Generic;
using Verse;

namespace PressR.Features.DirectHaul
{
    public enum DirectHaulStatus
    {
        None,
        Pending,
        Held,
    }

    public class State
    {
        public Map CurrentMap { get; internal set; }
        public List<Thing> SelectedThings { get; } = [];
        public Dictionary<Thing, IntVec3> GhostPlacements { get; } = [];

        public readonly Dictionary<
            Thing,
            (DirectHaulStatus Status, LocalTargetInfo TargetCell)
        > TrackedThings = [];

        public void Clear()
        {
            CurrentMap = null;
            SelectedThings.Clear();
            GhostPlacements.Clear();
        }
    }
}
