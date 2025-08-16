using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul
{
    public class PlacementService
    {
        private readonly HashSet<IntVec3> _bfsVisitedCells = [];
        private readonly Queue<IntVec3> _bfsQueue = new();
        private readonly List<IntVec3> _bfsLayerBuffer = [];
        private readonly List<CellMetric> _bfsLayerMetricBuffer = [];
        private readonly Dictionary<IntVec3, bool> _baseValidCellCache = [];
        private readonly HashSet<IntVec3> _reservedSetOwned = [];
        private ISet<IntVec3> _reservedSetCache;
        private readonly List<IntVec3> _bfsSeedBuffer = [];
        private readonly List<CellLineMetric> _bfsLayerLineMetricBuffer = [];
        private readonly List<CellLineMetric> _resamplingBuffer = [];
        private readonly HashSet<IntVec3> _seedUniqueSet = [];
        private const int BfsMaxSearchDepth = 100;
        private static readonly IntVec3[] NeighborDirections = GenAdj.AdjacentCells;
        private const int MultiSourceMaxSeedCount = 2048;
        private const int SeedAdjustMaxRadius = 6;
        private const float LineDistanceQuantizationScale = 1024f;
        private const float ProjectionQuantizationScale = 1_000_000f;
        private readonly List<IntVec3> _placementCells = [];

        private readonly struct CellMetric(IntVec3 cell, int dist2)
        {
            public readonly IntVec3 Cell = cell;
            public readonly int Dist2 = dist2;
        }

        private readonly struct CellLineMetric(
            IntVec3 cell,
            float dist2,
            int tiePrimary,
            int tieSecondary
        )
        {
            public readonly IntVec3 Cell = cell;
            public readonly float Dist2 = dist2;
            public readonly int TiePrimary = tiePrimary;
            public readonly int TieSecondary = tieSecondary;
        }

        private static readonly System.Comparison<CellLineMetric> CellLineMetricComparison =
            CompareCellLineMetrics;
        private static readonly System.Comparison<CellLineMetric> CellLineMetricProjectionComparison =
            CompareCellLineMetricsByProjection;

        private static readonly System.Comparison<CellMetric> CellMetricComparison =
            CompareCellMetrics;

        private static int CompareCellMetrics(CellMetric m1, CellMetric m2)
        {
            int c = m1.Dist2.CompareTo(m2.Dist2);
            if (c != 0)
            {
                return c;
            }
            int x = m1.Cell.x.CompareTo(m2.Cell.x);
            return x != 0 ? x : m1.Cell.z.CompareTo(m2.Cell.z);
        }

        private static int CompareCellLineMetrics(CellLineMetric m1, CellLineMetric m2)
        {
            int c = m1.Dist2.CompareTo(m2.Dist2);
            if (c != 0)
            {
                return c;
            }
            c = m1.TiePrimary.CompareTo(m2.TiePrimary);
            if (c != 0)
            {
                return c;
            }
            c = m1.TieSecondary.CompareTo(m2.TieSecondary);
            if (c != 0)
            {
                return c;
            }
            c = m1.Cell.x.CompareTo(m2.Cell.x);
            if (c != 0)
            {
                return c;
            }
            return m1.Cell.z.CompareTo(m2.Cell.z);
        }

        private static int CompareCellLineMetricsByProjection(CellLineMetric m1, CellLineMetric m2)
        {
            int c = m1.TiePrimary.CompareTo(m2.TiePrimary);
            if (c != 0)
            {
                return c;
            }
            c = m1.TieSecondary.CompareTo(m2.TieSecondary);
            if (c != 0)
            {
                return c;
            }
            c = m1.Cell.x.CompareTo(m2.Cell.x);
            if (c != 0)
            {
                return c;
            }
            return m1.Cell.z.CompareTo(m2.Cell.z);
        }

        public Dictionary<Thing, IntVec3> Calculate(PlacementRequest request)
        {
            if (!IsCalculateInputValid(request.Focus1, request.Map, request.ThingsToPlace.Count))
            {
                return [];
            }
            PreparePerCallCaches(request);

            List<IntVec3> placementCells = CalculatePlacementCells(request);

            if (placementCells.Count > 0)
            {
                return MapThingsToPlacementCells(request.ThingsToPlace, placementCells);
            }
            return [];
        }

        private static bool IsCalculateInputValid(IntVec3 focus, Map map, int requiredCount) =>
            map != null && requiredCount > 0 && focus.IsValid && focus.InBounds(map);

        private void PreparePerCallCaches(PlacementRequest request)
        {
            _baseValidCellCache.Clear();
            _reservedSetCache = AdaptToSetOrCopy(request.ReservedCells);
        }

        private ISet<IntVec3> AdaptToSetOrCopy(IReadOnlyCollection<IntVec3> source)
        {
            if (source == null)
            {
                return null;
            }
            if (source is ISet<IntVec3> set)
            {
                return set;
            }
            _reservedSetOwned.Clear();
            foreach (var c in source)
            {
                _reservedSetOwned.Add(c);
            }
            return _reservedSetOwned;
        }

        private List<IntVec3> CalculatePlacementCells(PlacementRequest request)
        {
            int required = request.ThingsToPlace.Count;
            var placementCells = _placementCells;
            placementCells.Clear();
            if (placementCells.Capacity < required)
            {
                placementCells.Capacity = required;
            }

            BuildSeedCells(request);
            InitializeBfsSeeds(_bfsSeedBuffer, request.Map);

            if (_bfsQueue.Count == 0)
            {
                return placementCells;
            }

            int depth = 0;
            while (
                _bfsQueue.Count > 0 && placementCells.Count < required && depth < BfsMaxSearchDepth
            )
            {
                ProcessBfsLayer(placementCells, request);
                depth++;
            }

            return placementCells;
        }

        private void BuildSeedCells(PlacementRequest request)
        {
            _bfsSeedBuffer.Clear();
            _seedUniqueSet.Clear();
            if (request.Focus1 == request.Focus2)
            {
                AddSeedIfUnique(request.Focus1);
                return;
            }

            var lineCells = GenSight.BresenhamCellsBetween(request.Focus1, request.Focus2);
            BuildLineSeeds(lineCells, request.ThingsToPlace.Count);
        }

        private void AddSeedIfUnique(IntVec3 cell)
        {
            if (_seedUniqueSet.Add(cell))
            {
                _bfsSeedBuffer.Add(cell);
            }
        }

        private void BuildLineSeeds(List<IntVec3> lineCells, int desiredCount)
        {
            int available = lineCells.Count;
            int seedCount = Mathf.Clamp(
                Mathf.Min(desiredCount, available),
                2,
                MultiSourceMaxSeedCount
            );

            if (seedCount == available)
            {
                for (int i = 0; i < available; i++)
                {
                    AddSeedIfUnique(lineCells[i]);
                }
                return;
            }

            for (int i = 0; i < seedCount; i++)
            {
                float t = (i + 0.5f) / seedCount;
                int idx = Mathf.Clamp(Mathf.RoundToInt(t * (available - 1)), 0, available - 1);
                AddSeedIfUnique(lineCells[idx]);
            }
        }

        private void InitializeBfsSeeds(List<IntVec3> seeds, Map map)
        {
            _bfsVisitedCells.Clear();
            _bfsQueue.Clear();

            for (int i = 0; i < seeds.Count; i++)
            {
                IntVec3 seed = seeds[i];
                IntVec3 enqueueCell = IsValidBfsTraversalCell(seed, map)
                    ? seed
                    : FindNearestTraversalCell(seed, map, SeedAdjustMaxRadius);

                if (enqueueCell.IsValid && _bfsVisitedCells.Add(enqueueCell))
                {
                    _bfsQueue.Enqueue(enqueueCell);
                }
            }
        }

        private IntVec3 FindNearestTraversalCell(IntVec3 center, Map map, int maxRadius)
        {
            if (IsValidBfsTraversalCell(center, map))
            {
                return center;
            }
            for (int r = 1; r <= maxRadius; r++)
            {
                int n = GenRadial.NumCellsInRadius(r);
                for (int i = 0; i < n; i++)
                {
                    IntVec3 c = center + GenRadial.RadialPattern[i];
                    if (c.InBounds(map) && IsValidBfsTraversalCell(c, map))
                    {
                        return c;
                    }
                }
            }
            return IntVec3.Invalid;
        }

        private void ProcessBfsLayer(List<IntVec3> placementCells, PlacementRequest request)
        {
            _bfsLayerBuffer.Clear();
            int countInLayer = _bfsQueue.Count;
            if (_bfsLayerBuffer.Capacity < countInLayer)
            {
                _bfsLayerBuffer.Capacity = countInLayer;
            }

            for (int i = 0; i < countInLayer; i++)
            {
                IntVec3 currentCell = _bfsQueue.Dequeue();

                if (IsCellValidForPlacement(currentCell, request, null))
                {
                    _bfsLayerBuffer.Add(currentCell);
                }

                for (int d = 0; d < NeighborDirections.Length; d++)
                {
                    var neighbor = currentCell + NeighborDirections[d];
                    TryEnqueueWalkableNeighbor(neighbor, request.Map);
                }
            }

            int take = request.ThingsToPlace.Count - placementCells.Count;
            SortLayerCellsByHeuristic(_bfsLayerBuffer, request, take);
            AddLayerCellsToResult(_bfsLayerBuffer, placementCells, request.ThingsToPlace.Count);
        }

        private void SortLayerCellsByHeuristic(
            List<IntVec3> layerCells,
            PlacementRequest request,
            int take
        )
        {
            if (request.Focus1 == request.Focus2)
            {
                SortLayerCellsByDistanceCached(layerCells, request.Focus1);
                return;
            }
            SortLayerCellsByDistanceToSegment(layerCells, request.Focus1, request.Focus2, take);
        }

        private void SortLayerCellsByDistanceToSegment(
            List<IntVec3> layerCells,
            IntVec3 aCell,
            IntVec3 bCell,
            int take
        )
        {
            int count = layerCells.Count;
            PrepareBuffer(_bfsLayerLineMetricBuffer, count);

            bool isHorizontal = aCell.z == bCell.z;
            bool isVertical = aCell.x == bCell.x;

            Vector3 a = aCell.ToVector3Shifted();
            Vector3 b = bCell.ToVector3Shifted();
            Vector3 ab = b - a;
            float ab2 = Vector3.Dot(ab, ab);

            for (int i = 0; i < count; i++)
            {
                var cell = layerCells[i];
                Vector3 p = cell.ToVector3Shifted();

                float t = ComputeClampedProjectionT(p, a, ab, ab2);
                float d2 = QuantizeLineDistance(
                    ComputeDistanceSquaredToPointProjection(p, a, ab, t)
                );

                int tiePrimary;
                int tieSecondary;
                if (isHorizontal)
                {
                    tiePrimary = cell.x;
                    tieSecondary = cell.z;
                }
                else if (isVertical)
                {
                    tiePrimary = cell.z;
                    tieSecondary = cell.x;
                }
                else
                {
                    int tFixed = Mathf.RoundToInt(t * ProjectionQuantizationScale);
                    int centerBiasFixed = Mathf.RoundToInt(
                        Mathf.Abs(t - 0.5f) * ProjectionQuantizationScale
                    );
                    tiePrimary = tFixed;
                    tieSecondary = centerBiasFixed;
                }

                _bfsLayerLineMetricBuffer.Add(
                    new CellLineMetric(cell, d2, tiePrimary, tieSecondary)
                );
            }

            _bfsLayerLineMetricBuffer.Sort(CellLineMetricComparison);

            if (take > 0 && take < count && (isHorizontal || isVertical))
            {
                ResampleSortedLayerForStraightLine(
                    layerCells,
                    take,
                    isHorizontal,
                    isHorizontal ? aCell.z : aCell.x
                );
                return;
            }
            if (!isHorizontal && !isVertical)
            {
                CopySortedBufferToResultOrderedByProjection(layerCells, take);
                return;
            }

            CopySortedBufferToResult(layerCells);
        }

        private void ResampleSortedLayerForStraightLine(
            List<IntVec3> layerCells,
            int take,
            bool isHorizontal,
            int axisValue
        )
        {
            int count = _bfsLayerLineMetricBuffer.Count;
            PrepareBuffer(layerCells, take);

            int centralCount = 0;
            while (
                centralCount < count
                && _bfsLayerLineMetricBuffer[centralCount].Dist2 == 0f
                && (
                    isHorizontal
                        ? _bfsLayerLineMetricBuffer[centralCount].Cell.z == axisValue
                        : _bfsLayerLineMetricBuffer[centralCount].Cell.x == axisValue
                )
            )
            {
                centralCount++;
            }

            if (centralCount >= take && centralCount > 0)
            {
                float stepCentral = (float)(centralCount - 1) / Mathf.Max(1, take - 1);
                for (int i = 0; i < take; i++)
                {
                    int idx = Mathf.Clamp(Mathf.RoundToInt(i * stepCentral), 0, centralCount - 1);
                    layerCells.Add(_bfsLayerLineMetricBuffer[idx].Cell);
                }
                return;
            }

            float step = (float)(count - 1) / Mathf.Max(1, take - 1);
            for (int i = 0; i < take; i++)
            {
                int idx = Mathf.Clamp(Mathf.RoundToInt(i * step), 0, count - 1);
                layerCells.Add(_bfsLayerLineMetricBuffer[idx].Cell);
            }
        }

        private void CopySortedBufferToResult(List<IntVec3> layerCells)
        {
            int count = _bfsLayerLineMetricBuffer.Count;
            PrepareBuffer(layerCells, count);
            for (int i = 0; i < count; i++)
            {
                layerCells.Add(_bfsLayerLineMetricBuffer[i].Cell);
            }
        }

        private void CopySortedBufferToResultOrderedByProjection(List<IntVec3> layerCells, int take)
        {
            int count = _bfsLayerLineMetricBuffer.Count;
            int copyCount = take > 0 && take < count ? take : count;

            var temp = _resamplingBuffer;
            PrepareBuffer(temp, copyCount);
            for (int i = 0; i < copyCount; i++)
            {
                temp.Add(_bfsLayerLineMetricBuffer[i]);
            }

            temp.Sort(CellLineMetricProjectionComparison);

            PrepareBuffer(layerCells, copyCount);
            for (int i = 0; i < copyCount; i++)
            {
                layerCells.Add(temp[i].Cell);
            }
        }

        private static float ComputeClampedProjectionT(
            Vector3 point,
            Vector3 segmentStart,
            Vector3 segmentVector,
            float segmentVectorLengthSquared
        )
        {
            if (segmentVectorLengthSquared <= 1e-6f)
            {
                return 0f;
            }
            float t = Vector3.Dot(point - segmentStart, segmentVector) / segmentVectorLengthSquared;
            return Mathf.Clamp01(t);
        }

        private static float ComputeDistanceSquaredToPointProjection(
            Vector3 point,
            Vector3 segmentStart,
            Vector3 segmentVector,
            float t
        )
        {
            Vector3 projection = segmentStart + segmentVector * t;
            return (point - projection).sqrMagnitude;
        }

        private static float QuantizeLineDistance(float distanceSquared)
        {
            return Mathf.Round(distanceSquared * LineDistanceQuantizationScale);
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

        private bool IsCellValidForPlacement(
            IntVec3 cell,
            PlacementRequest request,
            ISet<IntVec3> takenCells
        )
        {
            if (!IsBaseCellValid(cell, request.Map))
            {
                return false;
            }
            if (takenCells != null && takenCells.Contains(cell))
            {
                return false;
            }
            return true;
        }

        private bool IsBaseCellValid(IntVec3 cell, Map map)
        {
            if (_baseValidCellCache.TryGetValue(cell, out var valid))
            {
                return valid;
            }

            bool isValid = CheckCellValidity(cell, map);
            _baseValidCellCache[cell] = isValid;
            return isValid;
        }

        private bool CheckCellValidity(IntVec3 cell, Map map)
        {
            return IsInBoundsAndAccessible(cell, map)
                && !cell.InNoZoneEdgeArea(map)
                && !IsReserved(cell)
                && IsStandable(cell, map)
                && !HasEdifice(cell, map)
                && !IsUnreachableEdgeCell(cell, map)
                && !HasHaulPlaceBlocker(cell, map);
        }

        private static bool IsInBoundsAndAccessible(IntVec3 cell, Map map) =>
            cell.InBounds(map) && !cell.Fogged(map) && !cell.ContainsStaticFire(map);

        private bool IsReserved(IntVec3 cell) =>
            _reservedSetCache != null && _reservedSetCache.Contains(cell);

        private static bool IsStandable(IntVec3 cell, Map map) => cell.Standable(map);

        private static bool HasEdifice(IntVec3 cell, Map map) => map.edificeGrid[cell] != null;

        private static bool IsUnreachableEdgeCell(IntVec3 cell, Map map) =>
            cell.OnEdge(map)
            && !map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors));

        private static bool HasHaulPlaceBlocker(IntVec3 cell, Map map) =>
            GenPlace.HaulPlaceBlockerIn(null, cell, map, checkBlueprintsAndFrames: true) != null;

        private void SortLayerCellsByDistanceCached(List<IntVec3> layerCells, IntVec3 center)
        {
            int count = layerCells.Count;
            PrepareBuffer(_bfsLayerMetricBuffer, count);

            int cx = center.x;
            int cz = center.z;
            for (int i = 0; i < count; i++)
            {
                var cell = layerCells[i];
                int dx = cell.x - cx;
                int dz = cell.z - cz;
                int d2 = dx * dx + dz * dz;
                _bfsLayerMetricBuffer.Add(new CellMetric(cell, d2));
            }
            _bfsLayerMetricBuffer.Sort(CellMetricComparison);
            CopySortedMetricBufferToResult(layerCells);
        }

        private void CopySortedMetricBufferToResult(List<IntVec3> layerCells)
        {
            int count = _bfsLayerMetricBuffer.Count;
            PrepareBuffer(layerCells, count);
            for (int i = 0; i < _bfsLayerMetricBuffer.Count; i++)
            {
                layerCells.Add(_bfsLayerMetricBuffer[i].Cell);
            }
        }

        private static void PrepareBuffer<T>(List<T> buffer, int requiredCapacity)
        {
            buffer.Clear();
            if (buffer.Capacity < requiredCapacity)
            {
                buffer.Capacity = requiredCapacity;
            }
        }

        private static void AddLayerCellsToResult(
            List<IntVec3> layerCells,
            List<IntVec3> placementCells,
            int requiredCount
        )
        {
            int take = requiredCount - placementCells.Count;
            if (take <= 0)
            {
                return;
            }
            int count = layerCells.Count;
            if (take > count)
            {
                take = count;
            }
            for (int i = 0; i < take; i++)
            {
                placementCells.Add(layerCells[i]);
            }
        }

        private static bool IsValidBfsTraversalCell(IntVec3 cell, Map map)
        {
            return !cell.InNoZoneEdgeArea(map)
                && cell.Standable(map)
                && map.edificeGrid[cell] == null;
        }

        private static Dictionary<Thing, IntVec3> MapThingsToPlacementCells(
            IReadOnlyList<Thing> things,
            IReadOnlyList<IntVec3> cells
        )
        {
            if (things == null || cells == null)
            {
                return [];
            }
            int count = Mathf.Min(things.Count, cells.Count);
            var dict = new Dictionary<Thing, IntVec3>(count);
            for (int i = 0; i < count; i++)
            {
                var thing = things[i];
                if (thing != null)
                {
                    dict[thing] = cells[i];
                }
            }
            return dict;
        }
    }
}
