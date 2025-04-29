using System;
using PressR.Graphics;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.Graphics.GraphicObjects
{
    public class TabLensThingOverlayGraphicObject(Thing targetThing, Shader overlayShader = null)
        : IGraphicObject,
            IHasColor,
            IHasAlpha
    {
        private readonly Thing _targetThing =
            targetThing ?? throw new ArgumentNullException(nameof(targetThing));
        private Material _overlayMaterial;
        private Texture _lastOriginalMainTexture = null;
        private Color _originalMaterialColor = Color.white;
        private Mesh _currentMesh;
        private Matrix4x4 _baseMatrix;
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private bool _disposed = false;

        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public Color Color { get; set; } = Color.white;
        public float Alpha { get; set; } = 1f;

        public object Key => (_targetThing, GetType());

        public Shader OverlayShader { get; set; } = overlayShader;

        public void Update()
        {
            if (_disposed)
                return;

            if (!IsValid)
            {
                State = GraphicObjectState.PendingRemoval;
                return;
            }

            var renderData = ThingRenderDataReplicator.GetRenderData(
                _targetThing,
                returnOriginalMaterial: true
            );

            _currentMesh = renderData.Mesh;
            _baseMatrix = renderData.Matrix;
            Material originalMaterial = renderData.Material;

            if (originalMaterial == null)
            {
                State = GraphicObjectState.PendingRemoval;
                return;
            }

            _originalMaterialColor = originalMaterial.color;

            Shader shaderToUse = this.OverlayShader ?? ShaderManager.HSVColorizeCutoutShader;
            if (shaderToUse == null)
            {
                State = GraphicObjectState.PendingRemoval;
                return;
            }

            bool needsRecreation =
                _overlayMaterial == null
                || originalMaterial.mainTexture != _lastOriginalMainTexture
                || (OverlayShader != null && _overlayMaterial.shader != OverlayShader);

            if (needsRecreation)
            {
                if (_overlayMaterial != null)
                {
                    UnityEngine.Object.Destroy(_overlayMaterial);
                }

                _overlayMaterial = new Material(originalMaterial) { shader = shaderToUse };
                _lastOriginalMainTexture = originalMaterial.mainTexture;
            }
        }

        public void Render()
        {
            if (_disposed || !IsValid)
                return;

            if (_currentMesh == null || _overlayMaterial == null || _overlayMaterial.shader == null)
            {
                return;
            }

            IMpbConfigurator configurator = ShaderManager.GetConfigurator(_overlayMaterial.shader);

            _propertyBlock.Clear();
            if (configurator != null)
            {
                var payload = new MpbConfigurators.Payload
                {
                    TargetColor = this.Color,
                    OriginalBaseColor = _originalMaterialColor,
                    SaturationBlendFactor = 0.5f,
                    BrightnessBlendFactor = 0.0f,
                    Cutoff = 0.5f,
                    EffectBlendFactor = this.Alpha,
                };
                configurator.Configure(_propertyBlock, payload);
            }

            Vector3 finalPosition =
                (Vector3)_baseMatrix.GetColumn(3) + new Vector3(0f, Altitudes.AltInc, 0f);
            Matrix4x4 finalMatrix = Matrix4x4.TRS(
                finalPosition,
                _baseMatrix.rotation,
                _baseMatrix.lossyScale
            );

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

        private bool IsValid =>
            _targetThing != null
            && !_targetThing.Destroyed
            && _targetThing.SpawnedOrAnyParentSpawned;

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
