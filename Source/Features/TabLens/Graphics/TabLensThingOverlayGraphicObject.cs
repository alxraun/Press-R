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
        private Matrix4x4 _baseMatrix;
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private bool _disposed = false;

        private readonly ThingRenderStateCache _renderStateCache;
        private Material _lastUsedOriginalMaterial = null;

        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;

        public Color Color { get; set; } = Color.white;
        public float Alpha { get; set; } = 1f;

        public object Key => (_targetThing, GetType());

        public Shader OverlayShader { get; set; }

        public TabLensThingOverlayGraphicObject(Thing targetThing, Shader overlayShader = null)
        {
            _targetThing = targetThing ?? throw new ArgumentNullException(nameof(targetThing));
            this.OverlayShader = overlayShader;
            _renderStateCache = new ThingRenderStateCache(
                _targetThing,
                IsRenderUpdateRequiredForTabLens
            );
        }

        private static bool IsRenderUpdateRequiredForTabLens(
            Thing thing,
            ThingRenderStateCache cache
        )
        {
            if (cache.LastUpdateTick == -1)
                return true;
            if (!thing.SpawnedOrAnyParentSpawned)
                return true;

            IThingHolder currentParentHolder = thing.ParentHolder;
            Pawn currentCarrierPawn = (currentParentHolder as Pawn_CarryTracker)?.pawn;
            if (currentCarrierPawn != cache.LastCarrierPawn)
                return true;

            bool positionChanged;
            if (currentCarrierPawn != null)
            {
                positionChanged = currentCarrierPawn.DrawPos != cache.LastCarrierDrawPos;
            }
            else
            {
                positionChanged =
                    thing.Position != cache.LastPosition || thing.DrawPos != cache.LastDrawPos;
            }
            if (positionChanged)
                return true;

            if (thing.Rotation != cache.LastRotation)
                return true;

            int currentMapId = thing.Map?.uniqueID ?? -1;
            if (currentMapId != cache.LastMapId)
                return true;
            if (thing.stackCount != cache.LastStackCount)
                return true;
            if (thing.Stuff != cache.LastStuffDef)
                return true;
            if (currentParentHolder != cache.LastParentHolder)
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
            _originalMaterialColor = _lastUsedOriginalMaterial.color;

            _renderStateCache.RecordCurrentState();
            return true;
        }

        public void Update()
        {
            if (_disposed)
                return;
            if (!IsValid)
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
            {
                if (_overlayMaterial != null)
                    UnityEngine.Object.Destroy(_overlayMaterial);
                _overlayMaterial = null;
                _lastOriginalMainTexture = null;
                return;
            }

            Shader shaderToUse = this.OverlayShader ?? ShaderManager.HSVColorizeCutoutShader;
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
            if (_disposed || !IsValid)
                return;
            if (_currentMesh == null || _overlayMaterial == null || _overlayMaterial.shader == null)
                return;

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
