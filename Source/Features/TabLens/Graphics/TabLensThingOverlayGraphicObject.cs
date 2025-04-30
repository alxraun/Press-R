using System;
using PressR.Graphics;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.Graphics
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

        private int _lastUpdateTick = -1;
        private IntVec3 _lastPosition;
        private Rot4 _lastRotation;
        private int _lastMapId = -1;
        private int _lastStackCount = -1;
        private ThingDef _lastStuffDef = null;
        private Material _lastUsedOriginalMaterial = null;
        private IThingHolder _lastParentHolder = null;
        private Vector3 _lastDrawPos;
        private Vector3 _lastCarrierDrawPos;
        private Pawn _lastCarrierPawn = null;
        private IntVec3 _lastCarrierPosition;
        private Rot4 _lastCarrierRotation;

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

            bool needsRenderDataUpdate = CheckIfRenderDataUpdateIsNeeded();

            if (needsRenderDataUpdate)
            {
                var renderData = ThingRenderDataReplicator.GetRenderData(
                    _targetThing,
                    returnOriginalMaterial: true
                );

                if (renderData.Material == null)
                {
                    State = GraphicObjectState.PendingRemoval;
                    return;
                }

                _currentMesh = renderData.Mesh;
                _baseMatrix = renderData.Matrix;
                _lastUsedOriginalMaterial = renderData.Material;
                _originalMaterialColor = _lastUsedOriginalMaterial.color;

                UpdateCachedState();
            }

            UpdateOverlayMaterial(_lastUsedOriginalMaterial);
            if (_overlayMaterial == null)
            {
                State = GraphicObjectState.PendingRemoval;
                return;
            }
        }

        private bool CheckIfRenderDataUpdateIsNeeded()
        {
            int currentMapId = _targetThing.Map?.uniqueID ?? -1;
            IThingHolder currentParentHolder = _targetThing.ParentHolder;
            Pawn currentCarrierPawn = (currentParentHolder as Pawn_CarryTracker)?.pawn;

            // --- Check for carrier changes first ---
            if (currentCarrierPawn != _lastCarrierPawn) // Carrier appeared, disappeared, or changed
            {
                return true;
            }

            // --- Check position based on context ---
            bool positionChanged = false;
            if (currentCarrierPawn != null) // If carried
            {
                // Check the most accurate position data for the carrier
                if (currentCarrierPawn.DrawPos != _lastCarrierDrawPos)
                {
                    positionChanged = true;
                }
            }
            else // If not carried (on ground, in container, etc.)
            {
                // Check the thing's own positions
                if (_targetThing.Position != _lastPosition || _targetThing.DrawPos != _lastDrawPos)
                {
                    positionChanged = true;
                }
            }

            // --- Check other properties ---
            if (
                _lastUpdateTick == -1
                || positionChanged // Use the combined position check
                || _targetThing.Rotation != _lastRotation
                || currentMapId != _lastMapId
                || _targetThing.stackCount != _lastStackCount
                || _targetThing.Stuff != _lastStuffDef
                || currentParentHolder != _lastParentHolder // Keep this for container changes etc.
            )
            {
                return true;
            }

            return false;
        }

        private void UpdateCachedState()
        {
            _lastUpdateTick = GenTicks.TicksGame;
            _lastPosition = _targetThing.Position;
            _lastRotation = _targetThing.Rotation;
            _lastMapId = _targetThing.Map?.uniqueID ?? -1;
            _lastStackCount = _targetThing.stackCount;
            _lastStuffDef = _targetThing.Stuff;
            _lastParentHolder = _targetThing.ParentHolder;
            _lastDrawPos = _targetThing.DrawPos;
            _lastCarrierPawn = (_lastParentHolder as Pawn_CarryTracker)?.pawn;
            if (_lastCarrierPawn != null)
            {
                _lastCarrierPosition = _lastCarrierPawn.Position;
                _lastCarrierRotation = _lastCarrierPawn.Rotation;
                _lastCarrierDrawPos = _lastCarrierPawn.DrawPos;
            }
            else
            {
                // Reset carrier specific cache if not carried
                _lastCarrierPosition = IntVec3.Invalid;
                _lastCarrierRotation = Rot4.Invalid;
                _lastCarrierDrawPos = Vector3.zero;
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

            if (needsRecreation)
            {
                if (_overlayMaterial != null)
                {
                    UnityEngine.Object.Destroy(_overlayMaterial);
                }
                _overlayMaterial = new Material(originalMaterial) { shader = shaderToUse };
                _lastOriginalMainTexture = originalMaterial.mainTexture;
            }

            _originalMaterialColor = originalMaterial.color;
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
