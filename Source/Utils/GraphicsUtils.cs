using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PressR.Utils
{
    [StaticConstructorOnStartup]
    public static class GraphicsUtils
    {
        private static readonly Color AllowedColor = new Color(70f / 255f, 203f / 255f, 24f / 255f);
        private static readonly Color DisallowedColor = new Color(
            224f / 255f,
            60f / 255f,
            49f / 255f
        );
        private static BoolGrid _edgeGrid;

        public static Color GetAllowedColor() => AllowedColor;

        public static Color GetDisallowedColor() => DisallowedColor;

        public static Color GetColorForState(bool state)
        {
            return state ? AllowedColor : DisallowedColor;
        }

        public static void DrawThinFieldEdges(
            List<IntVec3> cells,
            Material lineMaterial,
            float? altOffset = null,
            float padding = 0f
        )
        {
            if (cells == null || !cells.Any() || lineMaterial == null)
                return;

            Map currentMap = Find.CurrentMap;
            if (currentMap == null)
                return;

            if (_edgeGrid == null)
                _edgeGrid = new BoolGrid(currentMap);
            else
                _edgeGrid.ClearAndResizeTo(currentMap);

            foreach (var cell in cells)
            {
                if (cell.InBounds(currentMap))
                    _edgeGrid[cell.x, cell.z] = true;
            }

            float yOffset = AltitudeLayer.MetaOverlays.AltitudeFor();

            foreach (var cell in cells)
            {
                if (!cell.InBounds(currentMap) || !_edgeGrid[cell.x, cell.z])
                    continue;

                IntVec3 cellPos = new IntVec3(cell.x, 0, cell.z);

                if (IsOuterEdgeTowards(cellPos, Rot4.South, currentMap, _edgeGrid))
                {
                    DrawEdgeSegment(
                        cellPos,
                        Rot4.South,
                        yOffset,
                        padding,
                        lineMaterial,
                        currentMap,
                        _edgeGrid
                    );
                }
                if (IsOuterEdgeTowards(cellPos, Rot4.North, currentMap, _edgeGrid))
                {
                    DrawEdgeSegment(
                        cellPos,
                        Rot4.North,
                        yOffset,
                        padding,
                        lineMaterial,
                        currentMap,
                        _edgeGrid
                    );
                }
                if (IsOuterEdgeTowards(cellPos, Rot4.West, currentMap, _edgeGrid))
                {
                    DrawEdgeSegment(
                        cellPos,
                        Rot4.West,
                        yOffset,
                        padding,
                        lineMaterial,
                        currentMap,
                        _edgeGrid
                    );
                }
                if (IsOuterEdgeTowards(cellPos, Rot4.East, currentMap, _edgeGrid))
                {
                    DrawEdgeSegment(
                        cellPos,
                        Rot4.East,
                        yOffset,
                        padding,
                        lineMaterial,
                        currentMap,
                        _edgeGrid
                    );
                }
            }
        }

        private static bool IsOuterEdgeTowards(
            IntVec3 cell,
            Rot4 direction,
            Map map,
            BoolGrid occupiedGrid
        )
        {
            IntVec3 neighbor = cell + direction.FacingCell;
            return !neighbor.InBounds(map) || !occupiedGrid[neighbor.x, neighbor.z];
        }

        private static void DrawEdgeSegment(
            IntVec3 cell,
            Rot4 direction,
            float yOffset,
            float padding,
            Material lineMaterial,
            Map map,
            BoolGrid occupiedGrid
        )
        {
            Vector3 startVertex,
                endVertex;
            Vector3 normalOffset;
            Rot4 perpendicularClockwise,
                perpendicularCounterClockwise;

            switch (direction.AsInt)
            {
                case 0:
                    startVertex = new Vector3(cell.x, yOffset, cell.z + 1);
                    endVertex = new Vector3(cell.x + 1, yOffset, cell.z + 1);
                    normalOffset = new Vector3(0, 0, padding);
                    perpendicularClockwise = Rot4.East;
                    perpendicularCounterClockwise = Rot4.West;
                    break;
                case 1:
                    startVertex = new Vector3(cell.x + 1, yOffset, cell.z + 1);
                    endVertex = new Vector3(cell.x + 1, yOffset, cell.z);
                    normalOffset = new Vector3(padding, 0, 0);
                    perpendicularClockwise = Rot4.South;
                    perpendicularCounterClockwise = Rot4.North;
                    break;
                case 2:
                    startVertex = new Vector3(cell.x + 1, yOffset, cell.z);
                    endVertex = new Vector3(cell.x, yOffset, cell.z);
                    normalOffset = new Vector3(0, 0, -padding);
                    perpendicularClockwise = Rot4.West;
                    perpendicularCounterClockwise = Rot4.East;
                    break;
                case 3:
                    startVertex = new Vector3(cell.x, yOffset, cell.z);
                    endVertex = new Vector3(cell.x, yOffset, cell.z + 1);
                    normalOffset = new Vector3(-padding, 0, 0);
                    perpendicularClockwise = Rot4.North;
                    perpendicularCounterClockwise = Rot4.South;
                    break;
                default:
                    return;
            }

            Vector3 p1 = startVertex + normalOffset;
            Vector3 p2 = endVertex + normalOffset;

            if (IsOuterEdgeTowards(cell, perpendicularCounterClockwise, map, occupiedGrid))
            {
                Vector3 cornerOffset = (
                    normalOffset
                    + GetNormalOffsetForDirection(perpendicularCounterClockwise, padding)
                );
                p1 = startVertex + cornerOffset;
            }

            if (IsOuterEdgeTowards(cell, perpendicularClockwise, map, occupiedGrid))
            {
                Vector3 cornerOffset = (
                    normalOffset + GetNormalOffsetForDirection(perpendicularClockwise, padding)
                );
                p2 = endVertex + cornerOffset;
            }

            GenDraw.DrawLineBetween(p1, p2, lineMaterial);
        }

        private static Vector3 GetNormalOffsetForDirection(Rot4 direction, float padding)
        {
            switch (direction.AsInt)
            {
                case 0:
                    return new Vector3(0, 0, padding);
                case 1:
                    return new Vector3(padding, 0, 0);
                case 2:
                    return new Vector3(0, 0, -padding);
                case 3:
                    return new Vector3(-padding, 0, 0);
                default:
                    return Vector3.zero;
            }
        }
    }
}
