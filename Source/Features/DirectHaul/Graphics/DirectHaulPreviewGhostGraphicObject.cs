using System;
using PressR.Graphics;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulPreviewGhostGraphicObject
        : IGraphicObject,
            IHasColor,
            IHasAlpha,
            IHasPosition
    {
        private readonly Thing _targetThing;
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private Mesh _currentMesh;
        private Matrix4x4 _baseMatrix;
        private Material _overlayMaterial;
        private Material _lastUsedOriginalMaterial = null;
        private Texture _lastOriginalMainTexture = null;
        private bool _disposed = false;

        private readonly ThingRenderStateCache _renderStateCache;

        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public Color Color { get; set; } = Color.white;
        public Color OutlineColor { get; set; } = Color.white;
        public float Cutoff { get; set; } = 0.5f;
        public float Alpha { get; set; } = 1f;
        public float EdgeSensitivity { get; set; } = 1.0f;
        public Vector3 Position { get; set; }

        public object Key => (_targetThing, GetType());

        public Shader Shader { get; set; }

        public DirectHaulPreviewGhostGraphicObject(
            Thing targetThing,
            Vector3? targetPosition = null,
            Shader shader = null
        )
        {
            _targetThing = targetThing ?? throw new ArgumentNullException(nameof(targetThing));
            Position = targetPosition ?? _targetThing.DrawPos;
            Shader = shader;

            _renderStateCache = new ThingRenderStateCache(
                _targetThing,
                IsRenderUpdateRequiredForGhost
            );
        }

        private static bool IsRenderUpdateRequiredForGhost(Thing thing, ThingRenderStateCache cache)
        {
            if (cache.LastUpdateTick == -1)
                return true;

            if (thing.Graphic != cache.LastGraphic)
                return true;
            if (thing.Stuff != cache.LastStuffDef)
                return true;
            return false;
        }

        public void OnRegistered() { }

        private bool TryRefreshCoreRenderData()
        {
            var renderData = ThingRenderDataReplicator.GetRenderData(
                _targetThing,
                returnOriginalMaterial: true
            );

            if (renderData.Material == null)
            {
                return false;
            }

            _currentMesh = renderData.Mesh;
            _baseMatrix = renderData.Matrix;
            _lastUsedOriginalMaterial = renderData.Material;

            _renderStateCache.RecordCurrentState();
            return true;
        }

        public void Update()
        {
            if (_disposed)
                return;

            if (!IsThingValidForUpdate())
            {
                State = GraphicObjectState.PendingRemoval;
                return;
            }

            if (_renderStateCache.IsUpdateNeeded())
            {
                if (!TryRefreshCoreRenderData())
                {
                    State = GraphicObjectState.PendingRemoval;
                    return;
                }
            }

            if (_lastUsedOriginalMaterial != null)
            {
                UpdateOverlayMaterial(_lastUsedOriginalMaterial);
            }
        }

        private void UpdateOverlayMaterial(Material originalMaterial)
        {
            if (originalMaterial == null)
                return;

            Shader shaderToUse = this.Shader;

            if (shaderToUse == null)
            {
                if (_overlayMaterial != null)
                    UnityEngine.Object.Destroy(_overlayMaterial);
                _overlayMaterial = null;
                _lastOriginalMainTexture = null;
                return;
            }

            bool needsRecreation =
                _overlayMaterial == null
                || originalMaterial.mainTexture != _lastOriginalMainTexture
                || _overlayMaterial.shader != shaderToUse;

            if (!needsRecreation)
                return;

            if (_overlayMaterial != null)
            {
                UnityEngine.Object.Destroy(_overlayMaterial);
            }
            _overlayMaterial = new Material(originalMaterial) { shader = shaderToUse };
            _lastOriginalMainTexture = originalMaterial.mainTexture;
        }

        public void Render()
        {
            if (_disposed || !IsRenderDataValid())
                return;

            Vector3 finalPosition = Position;
            finalPosition += new Vector3(0f, Altitudes.AltInc, 0f);

            Matrix4x4 finalMatrix = _baseMatrix;
            finalMatrix.SetColumn(
                3,
                new Vector4(finalPosition.x, finalPosition.y, finalPosition.z, 1f)
            );

            if (_overlayMaterial == null || _overlayMaterial.shader == null)
                return;

            IMpbConfigurator configurator = ShaderManager.GetConfigurator(_overlayMaterial.shader);

            _propertyBlock.Clear();
            if (configurator != null)
            {
                var payload = new MpbConfigurators.Payload
                {
                    FillColor = this.Color,
                    OutlineColor = this.OutlineColor,
                    Cutoff = this.Cutoff,
                    Alpha = this.Alpha,
                    EdgeSensitivity = this.EdgeSensitivity,
                };
                configurator.Configure(_propertyBlock, payload);
            }
            else
            {
                Color defaultColor = this.Color;
                defaultColor.a *= this.Alpha;
                _propertyBlock.SetColor(ShaderPropertyIDs.Color, defaultColor);
            }

            UnityEngine.Graphics.DrawMesh(
                _currentMesh,
                finalMatrix,
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
            GC.SuppressFinalize(this);
        }
    }
}
