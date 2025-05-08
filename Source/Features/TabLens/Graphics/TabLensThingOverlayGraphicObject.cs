using System;
using PressR.Graphics;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.Graphics
{
    public class TabLensThingOverlayGraphicObject : IGraphicObject, IHasColor, IHasAlpha
    {
        private readonly Thing _targetThing;
        private Material _overlayMaterial;
        private Texture _lastOriginalMainTexture = null;
        private Color _originalMaterialColor = Color.white;
        private Mesh _currentMesh;
        private Matrix4x4 _finalDrawMatrix;
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private bool _disposed = false;

        private readonly CachingThingRenderDataProvider _renderDataProvider;
        private Material _lastUsedOriginalMaterialFromProvider = null;

        private Color _lastAppliedColor = new Color(-1f, -1f, -1f, -1f);
        private float _lastAppliedAlpha = -1f;

        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private static readonly int OriginalBaseColorPropId = Shader.PropertyToID(
            "_OriginalBaseColor"
        );
        private static readonly int SaturationBlendFactorPropId = Shader.PropertyToID(
            "_SaturationBlendFactor"
        );
        private static readonly int BrightnessBlendFactorPropId = Shader.PropertyToID(
            "_BrightnessBlendFactor"
        );
        private static readonly int CutoffPropId = Shader.PropertyToID("_Cutoff");
        private static readonly int EffectBlendFactorPropId = Shader.PropertyToID(
            "_EffectBlendFactor"
        );

        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public Color Color { get; set; } = Color.white;
        public float Alpha { get; set; } = 1f;

        public object Key => (_targetThing, GetType());

        public TabLensThingOverlayGraphicObject(Thing targetThing)
        {
            _targetThing = targetThing ?? throw new ArgumentNullException(nameof(targetThing));

            TrackedStateParts partsForTabLens =
                TrackedStateParts.ParentHolder
                | TrackedStateParts.Position
                | TrackedStateParts.DrawPos
                | TrackedStateParts.Rotation
                | TrackedStateParts.StackCount
                | TrackedStateParts.Stuff;

            _renderDataProvider = new CachingThingRenderDataProvider(
                _targetThing,
                partsForTabLens,
                true
            );

            _propertyBlock.SetFloat(SaturationBlendFactorPropId, 0.5f);
            _propertyBlock.SetFloat(BrightnessBlendFactorPropId, 0.0f);
            _propertyBlock.SetFloat(CutoffPropId, 0.5f);
        }

        public void OnRegistered() { }

        private bool TrySetupAndConfigureHsvShader()
        {
            if (_overlayMaterial == null)
            {
                _lastOriginalMainTexture = null;

                _propertyBlock.SetColor(OriginalBaseColorPropId, Color.white);
                return false;
            }

            _overlayMaterial.shader = ShaderManager.HSVColorizeCutoutShader;

            if (_overlayMaterial.shader != ShaderManager.HSVColorizeCutoutShader)
            {
                _lastOriginalMainTexture = null;
                _propertyBlock.SetColor(OriginalBaseColorPropId, Color.white);
                return false;
            }

            _lastOriginalMainTexture = _overlayMaterial.mainTexture;
            _propertyBlock.SetColor(OriginalBaseColorPropId, _originalMaterialColor);
            return true;
        }

        public void Update()
        {
            if (_disposed)
            {
                return;
            }

            ThingRenderData currentRenderData = _renderDataProvider.GetRenderData();

            if (!IsValid)
            {
                State = GraphicObjectState.PendingRemoval;
                _overlayMaterial = null;
                _lastUsedOriginalMaterialFromProvider = null;
                _currentMesh = null;
                return;
            }

            _currentMesh = currentRenderData.Mesh;
            _finalDrawMatrix = currentRenderData.Matrix;
            _finalDrawMatrix.m13 += Altitudes.AltInc;

            if (_lastUsedOriginalMaterialFromProvider != currentRenderData.Material)
            {
                _lastUsedOriginalMaterialFromProvider = currentRenderData.Material;
                _overlayMaterial = _lastUsedOriginalMaterialFromProvider;
                _originalMaterialColor = _overlayMaterial.color;

                if (!TrySetupAndConfigureHsvShader())
                {
                    State = GraphicObjectState.PendingRemoval;
                    return;
                }
            }

            if (this.Color != _lastAppliedColor)
            {
                _propertyBlock.SetColor(ColorPropId, this.Color);
                _lastAppliedColor = this.Color;
            }
            if (this.Alpha != _lastAppliedAlpha)
            {
                _propertyBlock.SetFloat(EffectBlendFactorPropId, this.Alpha);
                _lastAppliedAlpha = this.Alpha;
            }
        }

        public void Render()
        {
            if (_disposed || !IsValid)
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

        private bool IsValid =>
            _targetThing != null
            && !_targetThing.Destroyed
            && _targetThing.SpawnedOrAnyParentSpawned;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _renderDataProvider?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
