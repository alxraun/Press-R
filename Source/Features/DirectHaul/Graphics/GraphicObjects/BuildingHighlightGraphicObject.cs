using System.Collections.Generic;
using System.Linq;
using PressR.Graphics.Interfaces;
using PressR.Utils;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics.GraphicObjects
{
    public class BuildingHighlightGraphicObject
        : IGraphicObject,
            IEffectTarget,
            IHasPadding,
            IHasAlpha,
            IHasColor,
            IHasTarget<Building>
    {
        private Building _target;
        private Material _lineMaterial;
        private List<IntVec3> _cachedCells = new List<IntVec3>();

        public Building Target
        {
            get => _target;
            set
            {
                if (_target == value)
                    return;
                _target = value;
                UpdateCells();
            }
        }

        public object Key => typeof(BuildingHighlightGraphicObject);
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;
        public float Padding { get; set; } = 0.0f;
        public float Alpha { get; set; } = 1f;
        public Color Color { get; set; } = Color.white;

        public BuildingHighlightGraphicObject(Building targetBuilding, Material lineMaterial = null)
        {
            Target =
                targetBuilding ?? throw new System.ArgumentNullException(nameof(targetBuilding));

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
            if (Target == null || !Target.Spawned || Target.Destroyed)
            {
                _cachedCells.Clear();
                return;
            }
        }

        private void UpdateCells()
        {
            if (Target != null && Target.Spawned)
            {
                _cachedCells = Target.OccupiedRect().Cells.ToList();
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
