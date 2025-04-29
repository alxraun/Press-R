using System.Linq;
using PressR.Graphics.GraphicObjects;
using PressR.Utils;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStorageRectGraphicObject : IGraphicObject, IHasAlpha, IHasColor
    {
        public static readonly object GraphicObjectId = new object();

        public object Key => GraphicObjectId;
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public IntVec3 StartCell { get; set; }
        public IntVec3 EndCell { get; set; }

        private const float DefaultFillAlpha = 0.09f;
        private const float DefaultEdgeAlpha = 1.0f;
        private const float EdgeColorToBaseFillLerpFactor = 0.05f;
        private const float MinAlphaForRender = 0.01f;

        public float Alpha { get; set; } = 1f;
        public Color Color { get; set; } = Color.white;

        private CellRect _currentRect = CellRect.Empty;

        private readonly MaterialPropertyBlock _mpb;
        private readonly Material _baseFillMaterial;
        private Material _lineMaterial;
        private Color _lastUsedEdgeColorRgb;
        private Color _finalFillColor;
        private Color _finalEdgeColor;

        public DirectHaulStorageRectGraphicObject(IntVec3 startCell, IntVec3 endCell)
        {
            StartCell = startCell;
            EndCell = endCell;

            _mpb = new MaterialPropertyBlock();
            _baseFillMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.white, true);
            _lastUsedEdgeColorRgb = new Color(Color.r, Color.g, Color.b, DefaultEdgeAlpha);

            _lineMaterial = MaterialPool.MatFrom(
                GenDraw.LineTexPath,
                ShaderDatabase.Transparent,
                _lastUsedEdgeColorRgb
            );
        }

        public void Update()
        {
            if (State != GraphicObjectState.Active || !StartCell.IsValid || !EndCell.IsValid)
            {
                _currentRect = CellRect.Empty;
                _finalFillColor = Color.clear;
                _finalEdgeColor = Color.clear;
                return;
            }

            _currentRect = CellRect.FromLimits(StartCell, EndCell);

            CalculateFinalColors();
            UpdateLineMaterialIfNeeded();
        }

        private void CalculateFinalColors()
        {
            Color baseEdgeColorRgb = new Color(Color.r, Color.g, Color.b, DefaultEdgeAlpha);
            Color blendedFillBase = Color.Lerp(
                baseEdgeColorRgb,
                Color.white,
                EdgeColorToBaseFillLerpFactor
            );

            _finalFillColor = new Color(
                blendedFillBase.r,
                blendedFillBase.g,
                blendedFillBase.b,
                DefaultFillAlpha * Alpha
            );
            _finalEdgeColor = new Color(
                baseEdgeColorRgb.r,
                baseEdgeColorRgb.g,
                baseEdgeColorRgb.b,
                DefaultEdgeAlpha * Alpha
            );
        }

        private void UpdateLineMaterialIfNeeded()
        {
            Color currentEdgeColorRgb = new Color(Color.r, Color.g, Color.b, DefaultEdgeAlpha);
            if (_lastUsedEdgeColorRgb == currentEdgeColorRgb)
            {
                return;
            }

            _lineMaterial = MaterialPool.MatFrom(
                GenDraw.LineTexPath,
                ShaderDatabase.Transparent,
                currentEdgeColorRgb
            );
            _lastUsedEdgeColorRgb = currentEdgeColorRgb;
        }

        public void Render()
        {
            if (_currentRect.Width <= 0)
            {
                return;
            }

            RenderFill();
            RenderEdges();
        }

        private void RenderFill()
        {
            if (_finalFillColor.a <= MinAlphaForRender)
            {
                return;
            }

            _mpb.Clear();
            _mpb.SetColor(ShaderPropertyIDs.Color, _finalFillColor);
            float altitude = AltitudeLayer.MetaOverlays.AltitudeFor();

            foreach (var cell in _currentRect.Cells)
            {
                Vector3 pos = cell.ToVector3ShiftedWithAltitude(altitude);
                UnityEngine.Graphics.DrawMesh(
                    MeshPool.plane10,
                    pos,
                    Quaternion.identity,
                    _baseFillMaterial,
                    0,
                    null,
                    0,
                    _mpb
                );
            }
        }

        private void RenderEdges()
        {
            if (_finalEdgeColor.a <= MinAlphaForRender || _lineMaterial == null)
            {
                return;
            }

            _lineMaterial.color = _finalEdgeColor;

            GraphicsUtils.DrawThinFieldEdges(_currentRect.Cells.ToList(), _lineMaterial);
        }

        public void Dispose() { }
    }
}
