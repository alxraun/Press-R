using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PressR.Features.DirectHaul.Core
{
    public sealed class DirectHaulFrameData
    {
        private DirectHaulExposableData _exposedData;
        private IntVec3 _lastFocus1 = IntVec3.Invalid;
        private IntVec3 _lastFocus2 = IntVec3.Invalid;
        private DirectHaulMode _lastCalculatedMode;
        private List<IntVec3> _calculatedPlacementCells = [];
        private readonly List<Thing> _allSelectedThings = [];
        private readonly List<Thing> _pendingSelectedThings = [];
        private readonly List<Thing> _heldSelectedThings = [];
        private readonly List<Thing> _nonPendingSelectedThings = [];
        private readonly List<Thing> _allPendingThingsOnMap = [];
        private readonly List<Thing> _allHeldThingsOnMap = [];
        private HashSet<IntVec3> _pendingTargetCells = [];

        public DirectHaulExposableData ExposedData => _exposedData;
        public IReadOnlyList<Thing> AllSelectedThings => _allSelectedThings.AsReadOnly();
        public IReadOnlyList<Thing> PendingSelectedThings => _pendingSelectedThings.AsReadOnly();
        public IReadOnlyList<Thing> HeldSelectedThings => _heldSelectedThings.AsReadOnly();
        public IReadOnlyList<Thing> NonPendingSelectedThings =>
            _nonPendingSelectedThings.AsReadOnly();
        public IReadOnlyList<Thing> AllPendingThingsOnMap => _allPendingThingsOnMap.AsReadOnly();
        public IReadOnlyList<Thing> AllHeldThingsOnMap => _allHeldThingsOnMap.AsReadOnly();
        public IReadOnlyCollection<IntVec3> PendingTargetCells => _pendingTargetCells;
        public IReadOnlyList<IntVec3> CalculatedPlacementCells =>
            _calculatedPlacementCells.AsReadOnly();

        public bool HasAnyPendingSelected => _pendingSelectedThings.Count > 0;
        public bool HasAnyHeldSelected => _heldSelectedThings.Count > 0;
        public bool HasAnyNonPendingSelected => _nonPendingSelectedThings.Count > 0;

        public void Update(Map map, IReadOnlyList<Thing> currentSelectedThings)
        {
            ClearState();
            LoadExposedData(map);

            if (currentSelectedThings == null || currentSelectedThings.Count == 0)
            {
                return;
            }

            PopulateSelectedThings(currentSelectedThings);
            LoadTrackedThingsFromExposedData();
            CategorizeSelectedThings();
        }

        public void UpdatePendingStatusLocally(IEnumerable<Thing> newlyPendingThings)
        {
            if (newlyPendingThings == null)
            {
                return;
            }

            var newlyPendingSet = newlyPendingThings.ToHashSet();
            if (newlyPendingSet.Count == 0)
            {
                return;
            }

            _nonPendingSelectedThings.RemoveAll(newlyPendingSet.Contains);
        }

        public void SetCalculatedPlacementCells(
            List<IntVec3> placementCells,
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulMode mode
        )
        {
            _calculatedPlacementCells = placementCells ?? [];
            _lastFocus1 = focus1;
            _lastFocus2 = focus2;
            _lastCalculatedMode = mode;
        }

        public bool HasValidCalculatedPlacementCells(
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulMode mode
        ) =>
            _calculatedPlacementCells.Count > 0
            && _lastFocus1 == focus1
            && _lastFocus2 == focus2
            && _lastCalculatedMode == mode;

        private void ClearState()
        {
            _exposedData = null;
            _allSelectedThings.Clear();
            _pendingSelectedThings.Clear();
            _heldSelectedThings.Clear();
            _nonPendingSelectedThings.Clear();
            _allPendingThingsOnMap.Clear();
            _allHeldThingsOnMap.Clear();
            _pendingTargetCells.Clear();
        }

        private void LoadExposedData(Map map)
        {
            _exposedData = map?.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
        }

        private void PopulateSelectedThings(IReadOnlyList<Thing> currentSelectedThings)
        {
            _allSelectedThings.AddRange(currentSelectedThings);
        }

        private void LoadTrackedThingsFromExposedData()
        {
            if (_exposedData == null)
            {
                return;
            }

            _allPendingThingsOnMap.AddRange(
                _exposedData.GetThingsWithStatus(DirectHaulStatus.Pending) ?? []
            );
            _allHeldThingsOnMap.AddRange(
                _exposedData.GetThingsWithStatus(DirectHaulStatus.Held) ?? []
            );
            _pendingTargetCells = _exposedData.GetPendingTargetCells() ?? [];
        }

        private void CategorizeSelectedThings()
        {
            if (_exposedData == null)
            {
                _nonPendingSelectedThings.AddRange(_allSelectedThings);
                return;
            }

            foreach (var thing in _allSelectedThings)
            {
                switch (_exposedData.GetStatusForThing(thing))
                {
                    case DirectHaulStatus.Pending:
                        _pendingSelectedThings.Add(thing);
                        break;
                    case DirectHaulStatus.Held:
                        _heldSelectedThings.Add(thing);
                        break;
                    default:
                        _nonPendingSelectedThings.Add(thing);
                        break;
                }
            }
        }
    }
}
