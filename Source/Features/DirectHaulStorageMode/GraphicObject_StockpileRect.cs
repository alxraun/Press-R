using PressR.Graphics;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaulStorageMode
{
    public sealed class GraphicObject_StockpileRect : IGraphicObject, IHasAlpha, IHasColor
    {
        public static readonly object GraphicObjectId = new();

        public object Key => GraphicObjectId;
        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public IntVec3 StartCell { get; set; }
        public IntVec3 EndCell { get; set; }

        public void OnRegistered()
        {
            ResetCaches();
        }

        private void ResetCaches()
        {
            _lastInputColor = new Color(-1f, -1f, -1f, -1f);
            _lastInputAlpha = -1f;
            _lastAppliedFillColor = new Color(-1f, -1f, -1f, -1f);
            _cachedMinX = int.MinValue;
            _cachedMinZ = int.MinValue;
            _cachedMaxX = int.MinValue;
            _cachedMaxZ = int.MinValue;
        }

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
        private Color _lastInputColor = new(-1f, -1f, -1f, -1f);
        private float _lastInputAlpha = -1f;
        private Color _lastAppliedFillColor = new(-1f, -1f, -1f, -1f);

        private Matrix4x4 _cachedFillMatrix;
        private Vector3 _edgeBL;
        private Vector3 _edgeTL;
        private Vector3 _edgeBR;
        private Vector3 _edgeTR;
        private int _cachedMinX = int.MinValue;
        private int _cachedMinZ = int.MinValue;
        private int _cachedMaxX = int.MinValue;
        private int _cachedMaxZ = int.MinValue;

        public GraphicObject_StockpileRect(IntVec3 startCell, IntVec3 endCell)
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
                _lastInputColor = new Color(-1f, -1f, -1f, -1f);
                _lastInputAlpha = -1f;
                _lastAppliedFillColor = new Color(-1f, -1f, -1f, -1f);
                _cachedMinX = int.MinValue;
                _cachedMinZ = int.MinValue;
                _cachedMaxX = int.MinValue;
                _cachedMaxZ = int.MinValue;
                return;
            }

            var map = Find.CurrentMap;
            if (map != null)
            {
                var allowedRect = map.BoundsRect(GenGrid.NoZoneEdgeWidth);
                _currentRect = CellRect.FromLimits(StartCell, EndCell).ClipInsideRect(allowedRect);
            }
            else
            {
                _currentRect = CellRect.FromLimits(StartCell, EndCell);
            }

            CalculateFinalColors();
            UpdateLineMaterialIfNeeded();
            UpdateCachedGeometryIfNeeded();
        }

        private void CalculateFinalColors()
        {
            if (_lastInputColor == Color && Mathf.Approximately(_lastInputAlpha, Alpha))
            {
                return;
            }

            Color baseEdgeColorRgb = new(Color.r, Color.g, Color.b, DefaultEdgeAlpha);
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

            _lastInputColor = Color;
            _lastInputAlpha = Alpha;
        }

        private void UpdateLineMaterialIfNeeded()
        {
            Color currentEdgeColorRgb = new(Color.r, Color.g, Color.b, DefaultEdgeAlpha);
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

        private void UpdateCachedGeometryIfNeeded()
        {
            int minX = _currentRect.minX;
            int maxX = _currentRect.maxX;
            int minZ = _currentRect.minZ;
            int maxZ = _currentRect.maxZ;

            if (
                minX == _cachedMinX
                && maxX == _cachedMaxX
                && minZ == _cachedMinZ
                && maxZ == _cachedMaxZ
            )
            {
                return;
            }

            _cachedMinX = minX;
            _cachedMaxX = maxX;
            _cachedMinZ = minZ;
            _cachedMaxZ = maxZ;

            float altitude = AltitudeLayer.MetaOverlays.AltitudeFor();

            int width = _currentRect.Width;
            int height = _currentRect.Height;

            Vector3 center = new(minX + (width * 0.5f), altitude, minZ + (height * 0.5f));

            Vector3 scale = new(width, 1f, height);
            _cachedFillMatrix = Matrix4x4.TRS(center, Quaternion.identity, scale);

            _edgeBL = new Vector3(minX, altitude, minZ);
            _edgeTL = new Vector3(minX, altitude, maxZ + 1);
            _edgeBR = new Vector3(maxX + 1, altitude, minZ);
            _edgeTR = new Vector3(maxX + 1, altitude, maxZ + 1);
        }

        public void Render()
        {
            if (_currentRect.Width <= 0 || _currentRect.Height <= 0)
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

            if (_finalFillColor != _lastAppliedFillColor)
            {
                _mpb.SetColor(ShaderPropertyIDs.Color, _finalFillColor);
                _lastAppliedFillColor = _finalFillColor;
            }

            UnityEngine.Graphics.DrawMesh(
                MeshPool.plane10,
                _cachedFillMatrix,
                _baseFillMaterial,
                0,
                null,
                0,
                _mpb
            );
        }

        private void RenderEdges()
        {
            if (_finalEdgeColor.a <= MinAlphaForRender || _lineMaterial == null)
            {
                return;
            }

            if (_lineMaterial.color != _finalEdgeColor)
            {
                _lineMaterial.color = _finalEdgeColor;
            }
            GenDraw.DrawLineBetween(_edgeBL, _edgeBR, _lineMaterial);
            GenDraw.DrawLineBetween(_edgeTL, _edgeTR, _lineMaterial);
            GenDraw.DrawLineBetween(_edgeBL, _edgeTL, _lineMaterial);
            GenDraw.DrawLineBetween(_edgeBR, _edgeTR, _lineMaterial);
        }

        public void Dispose() { }
    }
}
