using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul
{
    public sealed class DirectHaulPlacement
    {
        private List<IntVec3> _lastValidPlacementCells = [];
        private readonly HashSet<IntVec3> _bfsVisitedCells = [];
        private readonly Queue<IntVec3> _bfsQueue = new();
        private readonly List<IntVec3> _bfsLayerBuffer = [];
        private readonly HashSet<IntVec3> _interpolationUsedCells = [];

        private const float MaxDistanceForInterpolation = 15.0f;
        private const int BfsMaxSearchDepth = 100;
        private const int InterpolationMaxRadialSearchRadius = 5;
        private const int FillMaxSearchDepth = 10;
        private const int FillMaxCellsToProcess = 750;
        private static readonly IntVec3[] NeighborDirections = GenAdj.AdjacentCells;

        public List<IntVec3> FindPlacementCells(
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulFrameData frameData,
            Map map
        )
        {
            int requiredCount = frameData?.NonPendingSelectedThings?.Count ?? 0;
            if (!AreInputsValid(focus1, map, requiredCount))
            {
                _lastValidPlacementCells.Clear();
                return _lastValidPlacementCells;
            }

            List<IntVec3> currentPlacementCells =
                (focus1 == focus2)
                    ? CalculatePlacementCellsBfs(focus1, frameData, map, requiredCount)
                    : CalculatePlacementCellsInterpolated(
                        focus1,
                        focus2,
                        frameData,
                        map,
                        requiredCount
                    );

            return UpdateAndReturnCachedCells(currentPlacementCells, requiredCount);
        }

        private List<IntVec3> UpdateAndReturnCachedCells(
            List<IntVec3> currentCells,
            int requiredCount
        )
        {
            if (currentCells.Count > 0)
            {
                _lastValidPlacementCells = currentCells;
                return currentCells;
            }
            return _lastValidPlacementCells;
        }

        private static bool AreInputsValid(IntVec3 focus, Map map, int requiredCount) =>
            map != null && requiredCount > 0 && focus.IsValid && focus.InBounds(map);

        private List<IntVec3> CalculatePlacementCellsBfs(
            IntVec3 center,
            DirectHaulFrameData frameData,
            Map map,
            int requiredCount
        )
        {
            var placementCells = new List<IntVec3>(requiredCount);
            InitializeBfs(center, map);

            if (_bfsQueue.Count == 0)
                return placementCells;

            int depth = 0;
            while (
                _bfsQueue.Count > 0
                && placementCells.Count < requiredCount
                && depth < BfsMaxSearchDepth
            )
            {
                ProcessBfsLayer(placementCells, center, frameData, map, requiredCount);
                depth++;
            }

            return placementCells;
        }

        private void InitializeBfs(IntVec3 center, Map map)
        {
            _bfsVisitedCells.Clear();
            _bfsQueue.Clear();

            if (center.Standable(map) && _bfsVisitedCells.Add(center))
            {
                _bfsQueue.Enqueue(center);
                return;
            }

            foreach (var neighbor in GetNeighbors(center))
            {
                if (
                    neighbor.InBounds(map)
                    && neighbor.Standable(map)
                    && _bfsVisitedCells.Add(neighbor)
                )
                {
                    _bfsQueue.Enqueue(neighbor);
                }
            }
        }

        private void ProcessBfsLayer(
            List<IntVec3> placementCells,
            IntVec3 sortCenter,
            DirectHaulFrameData frameData,
            Map map,
            int requiredCount
        )
        {
            _bfsLayerBuffer.Clear();
            int countInLayer = _bfsQueue.Count;

            for (int i = 0; i < countInLayer; i++)
            {
                IntVec3 currentCell = _bfsQueue.Dequeue();

                if (IsCellValidForPlacement(currentCell, map, frameData, null))
                {
                    _bfsLayerBuffer.Add(currentCell);
                }

                foreach (var neighbor in GetNeighbors(currentCell))
                {
                    TryEnqueueWalkableNeighbor(neighbor, map);
                }
            }

            SortLayerCellsByDistance(_bfsLayerBuffer, sortCenter);
            AddLayerCellsToResult(_bfsLayerBuffer, placementCells, requiredCount);
        }

        private void TryEnqueueWalkableNeighbor(IntVec3 cell, Map map)
        {
            if (
                cell.InBounds(map)
                && _bfsVisitedCells.Add(cell)
                && IsValidBfsTraversalCell(cell, map)
            )
            {
                _bfsQueue.Enqueue(cell);
            }
        }

        private List<IntVec3> CalculatePlacementCellsInterpolated(
            IntVec3 focus1,
            IntVec3 focus2,
            DirectHaulFrameData frameData,
            Map map,
            int requiredCount
        )
        {
            var placementCells = new List<IntVec3>(requiredCount);
            _interpolationUsedCells.Clear();

            float distance = focus1.DistanceTo(focus2);
            float interpolationFactor = Mathf.Clamp01(distance / MaxDistanceForInterpolation);
            Vector3 startVec = focus1.ToVector3Shifted();
            Vector3 endVec = focus2.ToVector3Shifted();

            for (int i = 0; i < requiredCount; i++)
            {
                IntVec3 interpolatedCell = FindTargetCellViaInterpolation(
                    i,
                    requiredCount,
                    startVec,
                    endVec,
                    interpolationFactor
                );

                IntVec3 foundCell = FindNearestAvailableValidCell(
                    interpolatedCell,
                    map,
                    frameData,
                    _interpolationUsedCells,
                    InterpolationMaxRadialSearchRadius
                );

                if (foundCell.IsValid)
                {
                    placementCells.Add(foundCell);
                    _interpolationUsedCells.Add(foundCell);
                }
            }

            if (placementCells.Count < requiredCount)
            {
                TryFillRemainingCells(
                    placementCells,
                    _interpolationUsedCells,
                    map,
                    frameData,
                    requiredCount
                );
            }

            return placementCells;
        }

        private static IntVec3 FindTargetCellViaInterpolation(
            int index,
            int totalCount,
            Vector3 startVec,
            Vector3 endVec,
            float interpolationFactor
        )
        {
            float fractionAlongLine = (totalCount > 1) ? (index + 0.5f) / totalCount : 0.5f;
            Vector3 pointOnLine = Vector3.Lerp(startVec, endVec, fractionAlongLine);
            Vector3 interpolatedPoint = Vector3.Lerp(startVec, pointOnLine, interpolationFactor);
            return interpolatedPoint.ToIntVec3();
        }

        private static IntVec3 FindNearestAvailableValidCell(
            IntVec3 targetCell,
            Map map,
            DirectHaulFrameData frameData,
            ISet<IntVec3> usedCells,
            int maxRadius
        )
        {
            if (IsCellValidForPlacement(targetCell, map, frameData, usedCells))
            {
                return targetCell;
            }

            for (int radius = 1; radius <= maxRadius; ++radius)
            {
                IntVec3 foundCell = TryFindValidCellInRadialOffset(
                    targetCell,
                    map,
                    frameData,
                    usedCells,
                    radius
                );
                if (foundCell.IsValid)
                    return foundCell;
            }

            return IntVec3.Invalid;
        }

        private static IntVec3 TryFindValidCellInRadialOffset(
            IntVec3 center,
            Map map,
            DirectHaulFrameData frameData,
            ISet<IntVec3> usedCells,
            int radius
        )
        {
            int numCellsInRadius = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < numCellsInRadius; ++i)
            {
                IntVec3 checkCell = center + GenRadial.RadialPattern[i];
                if (IsCellValidForPlacement(checkCell, map, frameData, usedCells))
                {
                    return checkCell;
                }
            }
            return IntVec3.Invalid;
        }

        private static void TryFillRemainingCells(
            List<IntVec3> placementCells,
            ISet<IntVec3> usedCells,
            Map map,
            DirectHaulFrameData frameData,
            int requiredCount
        )
        {
            if (placementCells.Count >= requiredCount || placementCells.Count == 0)
                return;

            var fillQueue = new Queue<IntVec3>(placementCells);
            var visitedDuringFill = new HashSet<IntVec3>(usedCells);

            int depth = 0;
            int cellsProcessed = 0;

            while (
                fillQueue.Count > 0
                && placementCells.Count < requiredCount
                && depth < FillMaxSearchDepth
                && cellsProcessed < FillMaxCellsToProcess
            )
            {
                int currentLayerSize = fillQueue.Count;
                for (int i = 0; i < currentLayerSize; i++)
                {
                    if (placementCells.Count >= requiredCount)
                        return;

                    IntVec3 currentCell = fillQueue.Dequeue();
                    cellsProcessed++;

                    foreach (var neighbor in GetNeighbors(currentCell))
                    {
                        if (neighbor.InBounds(map) && visitedDuringFill.Add(neighbor))
                        {
                            if (IsCellValidForPlacement(neighbor, map, frameData, usedCells))
                            {
                                placementCells.Add(neighbor);
                                usedCells.Add(neighbor);
                                fillQueue.Enqueue(neighbor);

                                if (placementCells.Count >= requiredCount)
                                    return;
                            }
                        }
                    }
                }
                depth++;
            }
        }

        private static bool IsCellValidForPlacement(
            IntVec3 cell,
            Map map,
            DirectHaulFrameData frameData,
            ISet<IntVec3> dynamicallyUsedCells
        )
        {
            if (!cell.InBounds(map) || cell.Fogged(map) || cell.ContainsStaticFire(map))
                return false;

            if (dynamicallyUsedCells != null && dynamicallyUsedCells.Contains(cell))
                return false;

            if (!cell.Standable(map))
                return false;

            if (map.edificeGrid[cell] != null)
                return false;

            if (IsUnreachableMapEdge(cell, map))
                return false;

            if (IsPendingTargetCell(cell, frameData))
                return false;

            return !CellContainsBlockingThing(cell, map);
        }

        private static bool IsUnreachableMapEdge(IntVec3 cell, Map map) =>
            cell.OnEdge(map)
            && !map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors));

        private static bool IsPendingTargetCell(IntVec3 cell, DirectHaulFrameData frameData) =>
            frameData?.PendingTargetCells != null && frameData.PendingTargetCells.Contains(cell);

        private static bool CellContainsBlockingThing(IntVec3 cell, Map map)
        {
            List<Thing> thingsInCell = map.thingGrid.ThingsListAtFast(cell);
            return thingsInCell.Any(IsBlockingThing);
        }

        private static bool IsBlockingThing(Thing thing)
        {
            ThingDef def = thing.def;

            if (def.passability == Traversability.Impassable)
                return true;

            if (def.IsDoor)
                return true;

            if (def.category == ThingCategory.Item && def.EverHaulable)
                return true;

            BuildableDef buildDef = def.entityDefToBuild;
            if (buildDef != null && buildDef.passability == Traversability.Impassable)
                return true;

            if (def.category == ThingCategory.Building && IsImpassableSurfaceBuilding(thing))
                return true;

            return false;
        }

        private static bool IsImpassableSurfaceBuilding(Thing building) =>
            building.def.surfaceType == SurfaceType.None && !building.def.destroyable;

        private static IEnumerable<IntVec3> GetNeighbors(IntVec3 cell) =>
            NeighborDirections.Select(dir => cell + dir);

        private static void SortLayerCellsByDistance(List<IntVec3> layerCells, IntVec3 center) =>
            layerCells.Sort((a, b) => CompareCellsByDistanceThenPosition(a, b, center));

        private static int CompareCellsByDistanceThenPosition(IntVec3 a, IntVec3 b, IntVec3 center)
        {
            int distCompare = a.DistanceToSquared(center).CompareTo(b.DistanceToSquared(center));
            if (distCompare != 0)
                return distCompare;

            int xCompare = a.x.CompareTo(b.x);
            return (xCompare != 0) ? xCompare : a.z.CompareTo(b.z);
        }

        private static void AddLayerCellsToResult(
            IEnumerable<IntVec3> layerCells,
            List<IntVec3> placementCells,
            int requiredCount
        )
        {
            foreach (var cell in layerCells)
            {
                if (placementCells.Count >= requiredCount)
                    break;
                placementCells.Add(cell);
            }
        }

        private static bool IsValidBfsTraversalCell(IntVec3 cell, Map map)
        {
            return cell.Standable(map) && map.edificeGrid[cell] == null;
        }
    }
}
