using System.Collections.Generic;
using Verse;

namespace PressR.Features.DirectHaul
{
    public readonly struct PlacementRequest(
        IReadOnlyList<Thing> thingsToPlace,
        Map map,
        IntVec3 focus1,
        IntVec3 focus2,
        IReadOnlyCollection<IntVec3> reservedCells
    )
    {
        public readonly IReadOnlyList<Thing> ThingsToPlace = thingsToPlace;
        public readonly Map Map = map;
        public readonly IntVec3 Focus1 = focus1;
        public readonly IntVec3 Focus2 = focus2;
        public readonly IReadOnlyCollection<IntVec3> ReservedCells = reservedCells;
    }

    public class PlacementManager(
        PlacementService placementService,
        ThingStateManager thingStateManager,
        State state,
        Input input
    )
    {
        private readonly PlacementService _placementService = placementService;
        private readonly ThingStateManager _thingStateManager = thingStateManager;
        private readonly State _state = state;
        private readonly Input _input = input;

        private readonly List<Thing> _untrackedSelectedThings = [];
        private readonly List<int> _selectedThingIds = [];

        private IntVec3 _lastFocus1;
        private IntVec3 _lastFocus2;
        private int _lastSelectionHash;

        public void Update()
        {
            EnsureBufferCapacities(_state.SelectedThings.Count);

            if (!FillSelectionBuffers())
            {
                _state.GhostPlacements.Clear();
                return;
            }

            if (_input.IsDrag)
            {
                return;
            }

            GetCurrentFocus(out var currentFocus1, out var currentFocus2);

            int currentSelectionHash = ComputeSelectionHash();

            if (
                _lastFocus1 == currentFocus1
                && _lastFocus2 == currentFocus2
                && _lastSelectionHash == currentSelectionHash
            )
            {
                return;
            }

            var request = new PlacementRequest(
                _untrackedSelectedThings,
                _state.CurrentMap,
                currentFocus1,
                currentFocus2,
                _thingStateManager.PendingTargetCells
            );
            var placements = _placementService.Calculate(request);

            ApplyPlacements(placements);

            _lastFocus1 = currentFocus1;
            _lastFocus2 = currentFocus2;
            _lastSelectionHash = currentSelectionHash;
        }

        public void Reset()
        {
            _lastFocus1 = IntVec3.Invalid;
            _lastFocus2 = IntVec3.Invalid;
            _lastSelectionHash = -1;
        }

        private void EnsureBufferCapacities(int required)
        {
            if (_untrackedSelectedThings.Capacity < required)
            {
                _untrackedSelectedThings.Capacity = required;
            }
            if (_selectedThingIds.Capacity < required)
            {
                _selectedThingIds.Capacity = required;
            }
        }

        private bool FillSelectionBuffers()
        {
            _untrackedSelectedThings.Clear();
            _selectedThingIds.Clear();

            foreach (var thing in _thingStateManager.UntrackedSelectedThings)
            {
                _untrackedSelectedThings.Add(thing);
                _selectedThingIds.Add(thing.thingIDNumber);
            }

            return _untrackedSelectedThings.Count != 0;
        }

        private void GetCurrentFocus(out IntVec3 focus1, out IntVec3 focus2)
        {
            if (_input.IsDragging)
            {
                focus1 = _input.StartDragCell;
                focus2 = _input.CurrentDragCell;
                return;
            }

            focus1 = Verse.UI.MouseCell();
            focus2 = focus1;
        }

        private int ComputeSelectionHash()
        {
            if (_selectedThingIds.Count > 1)
            {
                _selectedThingIds.Sort();
            }

            int hash = 0;
            for (int i = 0; i < _selectedThingIds.Count; i++)
            {
                hash = Gen.HashCombineInt(hash, _selectedThingIds[i]);
            }
            return hash;
        }

        private void ApplyPlacements(Dictionary<Thing, IntVec3> placements)
        {
            _state.GhostPlacements.Clear();
            if (placements.Count == 0)
            {
                return;
            }

            foreach (var placement in placements)
            {
                _state.GhostPlacements[placement.Key] = placement.Value;
            }
        }
    }
}
