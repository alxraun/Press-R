using System;
using Microtools.Graphics;
using Microtools.Graphics.Replicator;
using UnityEngine;
using Verse;

namespace Microtools.Features.DirectHaul
{
    public enum GhostType
    {
        Preview,
        Pending,
    }

    public sealed class GraphicObject_Ghost : IGraphicObject, IHasColor, IHasAlpha, IHasPosition
    {
        private readonly Thing _targetThing;
        private readonly GhostType _ghostType;
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private Mesh _currentMesh;
        private Matrix4x4 _baseMatrix;
        private Matrix4x4 _finalDrawMatrix;
        private Material _overlayMaterial;
        private Material _lastUsedOriginalMaterialFromProvider = null;
        private bool _disposed = false;

        private readonly CachingThingRenderDataProvider _renderDataProvider;

        private static readonly int FillColorPropId = Shader.PropertyToID("_FillColor");
        private static readonly int OutlineColorPropId = Shader.PropertyToID("_OutlineColor");
        private static readonly int CutoffPropId = Shader.PropertyToID("_Cutoff");
        private static readonly int EdgeSensitivityPropId = Shader.PropertyToID("_EdgeSensitivity");
        private static readonly int EffectAlphaPropId = Shader.PropertyToID("_EffectAlpha");

        private Color _lastAppliedFillColor = new(-1f, -1f, -1f, -1f);
        private Color _lastAppliedOutlineColor = new(-1f, -1f, -1f, -1f);
        private float _lastAppliedCutoff = -1f;
        private float _lastAppliedAlpha = -1f;
        private float _lastAppliedEdgeSensitivity = -1f;

        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public Color Color { get; set; } = Color.white;
        public Color OutlineColor { get; set; } = Color.white;
        public float Cutoff { get; set; } = 0.5f;
        public float Alpha { get; set; } = 1f;
        public float EdgeSensitivity { get; set; } = 1.0f;
        public Vector3 Position { get; set; }

        public object Key => (_targetThing, _ghostType);

        public GraphicObject_Ghost(
            Thing targetThing,
            GhostType ghostType,
            Vector3? targetPosition = null
        )
        {
            _targetThing = targetThing ?? throw new ArgumentNullException(nameof(targetThing));
            _ghostType = ghostType;
            Position = targetPosition ?? _targetThing.DrawPos;

            TrackedStateParts partsToTrack =
                TrackedStateParts.Stuff | TrackedStateParts.DrawPos | TrackedStateParts.Rotation;
            _renderDataProvider = new CachingThingRenderDataProvider(
                _targetThing,
                partsToTrack,
                copyMaterialForClient: true
            );
        }

        public void OnRegistered() { }

        private bool TrySetupAndConfigureGhostShader()
        {
            if (_overlayMaterial == null)
            {
                return false;
            }

            _overlayMaterial.shader = ShaderManager.SobelEdgeDetectShader;

            if (_overlayMaterial.shader != ShaderManager.SobelEdgeDetectShader)
            {
                return false;
            }

            return true;
        }

        public void Update()
        {
            if (_disposed)
                return;

            if (!IsThingValidForUpdate())
            {
                State = GraphicObjectState.PendingRemoval;
                if (_overlayMaterial != null) { }
                _currentMesh = null;
                _overlayMaterial = null;
                _lastUsedOriginalMaterialFromProvider = null;
                return;
            }

            ThingRenderData currentRenderData = _renderDataProvider.GetRenderData();

            if (currentRenderData.Material == null && _targetThing.SpawnedOrAnyParentSpawned)
            {
                State = GraphicObjectState.PendingRemoval;
                _currentMesh = null;
                _overlayMaterial = null;
                _lastUsedOriginalMaterialFromProvider = null;
                return;
            }

            _currentMesh = currentRenderData.Mesh;
            _baseMatrix = currentRenderData.Matrix;

            if (_lastUsedOriginalMaterialFromProvider != currentRenderData.Material)
            {
                _lastUsedOriginalMaterialFromProvider = currentRenderData.Material;
                _overlayMaterial = _lastUsedOriginalMaterialFromProvider;

                if (!TrySetupAndConfigureGhostShader())
                {
                    State = GraphicObjectState.PendingRemoval;
                    return;
                }

                _lastAppliedFillColor = new Color(-1f, -1f, -1f, -1f);
                _lastAppliedOutlineColor = new Color(-1f, -1f, -1f, -1f);
                _lastAppliedCutoff = -1f;
                _lastAppliedAlpha = -1f;
                _lastAppliedEdgeSensitivity = -1f;
            }

            Color currentFillColor = this.Color;
            if (currentFillColor != _lastAppliedFillColor)
            {
                _propertyBlock.SetColor(FillColorPropId, currentFillColor);
                _lastAppliedFillColor = currentFillColor;
            }

            float currentAlpha = this.Alpha;
            if (currentAlpha != _lastAppliedAlpha)
            {
                _propertyBlock.SetFloat(EffectAlphaPropId, currentAlpha);
                _lastAppliedAlpha = currentAlpha;
            }

            if (this.OutlineColor != _lastAppliedOutlineColor)
            {
                _propertyBlock.SetColor(OutlineColorPropId, this.OutlineColor);
                _lastAppliedOutlineColor = this.OutlineColor;
            }

            if (this.Cutoff != _lastAppliedCutoff)
            {
                _propertyBlock.SetFloat(CutoffPropId, this.Cutoff);
                _lastAppliedCutoff = this.Cutoff;
            }

            if (this.EdgeSensitivity != _lastAppliedEdgeSensitivity)
            {
                _propertyBlock.SetFloat(EdgeSensitivityPropId, this.EdgeSensitivity);
                _lastAppliedEdgeSensitivity = this.EdgeSensitivity;
            }

            _finalDrawMatrix = _baseMatrix;
            _finalDrawMatrix.m03 = this.Position.x;
            _finalDrawMatrix.m13 = this.Position.y + Altitudes.AltInc;
            _finalDrawMatrix.m23 = this.Position.z;
        }

        public void Render()
        {
            if (
                _disposed
                || !IsRenderDataValid()
                || _overlayMaterial == null
                || _overlayMaterial.shader == null
            )
                return;

            UnityEngine.Graphics.DrawMesh(
                _currentMesh,
                _finalDrawMatrix,
                _overlayMaterial,
                0,
                null,
                0,
                _propertyBlock
            );
        }

        private bool IsThingValidForUpdate() =>
            _targetThing != null
            && !_targetThing.Destroyed
            && _targetThing.SpawnedOrAnyParentSpawned;

        private bool IsRenderDataValid() => _currentMesh != null && _overlayMaterial != null;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_overlayMaterial != null)
            {
                UnityEngine.Object.Destroy(_overlayMaterial);
                _overlayMaterial = null;
            }
            _renderDataProvider?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
