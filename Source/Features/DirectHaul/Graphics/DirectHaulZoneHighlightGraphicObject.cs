using System.Collections.Generic;
using System.Linq;
using PressR.Graphics.GraphicObjects;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulZoneHighlightGraphicObject
        : IGraphicObject,
            IEffectTarget,
            IHasPadding,
            IHasAlpha,
            IHasColor,
            IHasTarget<Zone_Stockpile>
    {
        private Zone_Stockpile _targetZone;
        private Material _lineMaterial;
        private List<IntVec3> _cachedCells = new List<IntVec3>();
        private Map _map;

        public Zone_Stockpile Target
        {
            get => _targetZone;
            set
            {
                if (_targetZone == value)
                    return;
                _targetZone = value;
                _map = _targetZone?.Map;
                UpdateCells();
            }
        }

        public object Key => typeof(DirectHaulZoneHighlightGraphicObject);
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;
        public float Padding { get; set; } = 0.1f;
        public float Alpha { get; set; } = 1f;
        public Color Color { get; set; } = Color.white;

        public DirectHaulZoneHighlightGraphicObject(
            Zone_Stockpile targetZone,
            Material lineMaterial = null
        )
        {
            Target = targetZone ?? throw new System.ArgumentNullException(nameof(targetZone));

            Material baseMat =
                lineMaterial
                ?? MaterialPool.MatFrom(
                    GenDraw.LineTexPath,
                    ShaderDatabase.MetaOverlay,
                    Color.white
                );
            _lineMaterial = new Material(baseMat);
            Color = _lineMaterial.color;
        }

        public void Update()
        {
            if (Target == null || _map == null || !_map.zoneManager.AllZones.Contains(Target))
            {
                _cachedCells.Clear();
                return;
            }

            if (Target.Cells.Count != _cachedCells.Count)
            {
                UpdateCells();
            }
        }

        private void UpdateCells()
        {
            if (Target != null && _map != null && _map.zoneManager.AllZones.Contains(Target))
            {
                _cachedCells = Target.Cells.ToList();
            }
            else
            {
                _cachedCells.Clear();
            }
        }

        public void Render()
        {
            if (!_cachedCells.Any())
            {
                return;
            }

            Color finalColor = this.Color;
            finalColor.a *= this.Alpha;
            _lineMaterial.color = finalColor;

            GraphicsUtils.DrawThinFieldEdges(_cachedCells, _lineMaterial, padding: Padding);
        }

        public void Dispose()
        {
            if (_lineMaterial != null)
            {
                UnityEngine.Object.Destroy(_lineMaterial);
                _lineMaterial = null;
            }
        }
    }
}
