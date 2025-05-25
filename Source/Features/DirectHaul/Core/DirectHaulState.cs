using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Core
{
    public class DirectHaulState
    {
        private enum DragStateKind
        {
            Idle,
            Dragging,
            Completed,
        }

        private DragStateKind _dragState = DragStateKind.Idle;
        private IntVec3 _startDragCell;
        private IntVec3 _currentDragCell;
        private float _dragDistance;
        private const float MinDragDistanceThreshold = 0.1f;

        public bool IsDragging => _dragState == DragStateKind.Dragging;
        public bool IsDragCompleted => _dragState == DragStateKind.Completed;
        public IntVec3 StartDragCell => _startDragCell;
        public IntVec3 CurrentDragCell => _currentDragCell;
        public float DragDistance => _dragDistance;

        public void StartDrag(IntVec3 cell)
        {
            _startDragCell = cell;
            _currentDragCell = cell;
            _dragDistance = 0f;
            _dragState = DragStateKind.Idle;
        }

        public void UpdateDrag(IntVec3 cell)
        {
            _currentDragCell = cell;
            _dragDistance = CalculateDragDistance(_startDragCell, _currentDragCell);

            if (_dragState == DragStateKind.Idle && _dragDistance >= MinDragDistanceThreshold)
                _dragState = DragStateKind.Dragging;
        }

        public void EndDrag()
        {
            if (_startDragCell.IsValid)
                _dragState = DragStateKind.Completed;
        }

        public void ResetDragState()
        {
            _dragState = DragStateKind.Idle;
            _startDragCell = IntVec3.Invalid;
            _currentDragCell = IntVec3.Invalid;
            _dragDistance = 0f;
        }

        private float CalculateDragDistance(IntVec3 start, IntVec3 end)
        {
            if (!start.IsValid || !end.IsValid)
            {
                return 0f;
            }

            return Mathf.Sqrt(Mathf.Pow(end.x - start.x, 2) + Mathf.Pow(end.z - start.z, 2));
        }

        private Map _map;
        private DirectHaulExposableData _exposedData;
        private IntVec3 _currentMouseCell;

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

        public Map Map => _map;
        public DirectHaulExposableData ExposedData => _exposedData;
        public IntVec3 CurrentMouseCell => _currentMouseCell;
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

        public DirectHaulMode Mode { get; private set; } = DirectHaulMode.Standard;
        public IStoreSettingsParent StorageUnderMouse { get; private set; } = null;

        public void SetCurrentMode(DirectHaulMode mode)
        {
            Mode = mode;
        }

        public void SetStorageUnderMouse(IStoreSettingsParent storage)
        {
            StorageUnderMouse = storage;
        }

        public void SetCurrentMouseCell(IntVec3 cell)
        {
            _currentMouseCell = cell;
        }

        public void UpdateSelectionAndTrackedData(
            Map currentMap,
            IReadOnlyList<Thing> currentSelectedThings
        )
        {
            ClearTransientSelectionData();
            _map = currentMap;

            LoadExposedData(currentMap);

            if (currentSelectedThings == null || currentSelectedThings.Count == 0)
            {
                return;
            }

            _allSelectedThings.AddRange(currentSelectedThings);

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

            _nonPendingSelectedThings.RemoveAll(thing => newlyPendingSet.Contains(thing));
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

        public bool MarkThingPending(
            Thing thing,
            LocalTargetInfo targetCell,
            bool isHighPriority = false
        )
        {
            if (thing == null || !targetCell.IsValid || _exposedData == null)
            {
                return false;
            }
            _exposedData.MarkThingAsPending(thing, targetCell, isHighPriority);
            return true;
        }

        public bool MarkThingHeld(Thing thing)
        {
            if (thing == null || _exposedData == null || !_exposedData.IsThingInTracking(thing))
            {
                return false;
            }
            _exposedData.MarkThingAsHeld(thing);
            return true;
        }

        public bool RemoveThingFromTracking(Thing thing)
        {
            if (thing == null || _exposedData == null)
            {
                return false;
            }
            _exposedData.RemoveThingFromTracking(thing);
            return true;
        }

        public void Clear()
        {
            ResetDragState();
            ClearTransientSelectionData();
            ClearPlacementCache();
            _map = null;
            _exposedData = null;
            _currentMouseCell = IntVec3.Invalid;
            Mode = DirectHaulMode.Standard;
            StorageUnderMouse = null;
        }

        private void ClearTransientSelectionData()
        {
            _allSelectedThings.Clear();
            _pendingSelectedThings.Clear();
            _heldSelectedThings.Clear();
            _nonPendingSelectedThings.Clear();
            _allPendingThingsOnMap.Clear();
            _allHeldThingsOnMap.Clear();
            _pendingTargetCells.Clear();
        }

        public void ClearPlacementCache()
        {
            _calculatedPlacementCells.Clear();
            _lastFocus1 = IntVec3.Invalid;
            _lastFocus2 = IntVec3.Invalid;
        }

        private void LoadExposedData(Map map)
        {
            _exposedData = map?.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
        }

        private void LoadTrackedThingsFromExposedData()
        {
            if (_exposedData == null)
            {
                return;
            }

            _allPendingThingsOnMap.AddRange(
                _exposedData.GetThingsWithStatus(DirectHaulStatus.Pending)
                    ?? Enumerable.Empty<Thing>()
            );
            _allHeldThingsOnMap.AddRange(
                _exposedData.GetThingsWithStatus(DirectHaulStatus.Held) ?? Enumerable.Empty<Thing>()
            );
            _pendingTargetCells = _exposedData.GetPendingTargetCells() ?? new HashSet<IntVec3>();
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

        public List<Thing> MarkThingsPending(
            IReadOnlyList<IntVec3> cells,
            bool isHighPriority = false
        )
        {
            if (
                _exposedData == null
                || !HasAnyNonPendingSelected
                || cells == null
                || cells.Count < _nonPendingSelectedThings.Count
            )
            {
                return [];
            }

            var successfullyMarked = new List<Thing>(_nonPendingSelectedThings.Count);
            for (int i = 0; i < _nonPendingSelectedThings.Count; i++)
            {
                Thing thing = _nonPendingSelectedThings[i];
                IntVec3 cell = cells[i];

                if (TryMarkSingleThingAsPending(thing, cell, _exposedData, isHighPriority))
                {
                    successfullyMarked.Add(thing);
                }
            }
            return successfullyMarked;
        }

        public int RemoveThingsFromTracking(IEnumerable<Thing> thingsToRemove)
        {
            if (_exposedData == null || thingsToRemove == null)
            {
                return 0;
            }

            return thingsToRemove.Count(thing =>
                TryRemoveSingleThingFromTracking(thing, _exposedData)
            );
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
