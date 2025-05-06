using PressR.Graphics.GraphicObjects;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStatusOverlayGraphicObject : IGraphicObject, IHasAlpha, IHasPosition
    {
        private readonly Thing _targetThing;
        private string _currentTexturePath;
        private const float overlayGraphicSize = 0.3f;

        private static readonly Vector3 BaseCarriedOffset = new Vector3(0.0f, 0.0f, -0.1f);
        private const float BaseCarriedNorthZOffset = 0.0f;
        private const float BaseCarriedEastXOffset = 0.18f;
        private const float BaseCarriedWestXOffset = -0.18f;
        private const float ChildOffsetZ = -0.1f;
        private const float VanillaCarriedYOffsetValue = 0.03846154f;

        public GraphicObjectState State { get; set; } = GraphicObjectState.Active;
        public object Key => (_targetThing, typeof(DirectHaulStatusOverlayGraphicObject));
        public float Alpha { get; set; } = 1.0f;
        public Vector3 Position { get; set; }

        private Material _cachedMaterial;
        private string _materialTexturePathUsedForCache;
        private Vector2 _cachedThingDrawSize;

        public DirectHaulStatusOverlayGraphicObject(Thing targetThing)
        {
            _targetThing =
                targetThing ?? throw new System.ArgumentNullException(nameof(targetThing));
            if (_targetThing.Graphic != null)
            {
                _cachedThingDrawSize = _targetThing.Graphic.drawSize;
            }
            else
            {
                _cachedThingDrawSize = Vector2.one;
            }
        }

        public void OnRegistered() { }

        public void UpdateVisualState(string texturePath)
        {
            _currentTexturePath = texturePath;
        }

        public void Update()
        {
            if (!IsValid(_targetThing))
            {
                State = GraphicObjectState.PendingRemoval;
                Position = Vector3.zero;
                return;
            }
            CalculatePosition();
        }

        private void CalculatePosition()
        {
            if (_targetThing == null)
            {
                Position = Vector3.zero;
                State = GraphicObjectState.PendingRemoval;
                return;
            }

            if (
                _targetThing.ParentHolder is Pawn_CarryTracker carryTracker
                && carryTracker.pawn != null
            )
            {
                Vector3? pawnBasedPosition = CalculatePositionWhenCarriedByPawn(carryTracker.pawn);
                if (pawnBasedPosition.HasValue)
                {
                    Position = pawnBasedPosition.Value;
                }
                else
                {
                    Position = Vector3.zero;
                    State = GraphicObjectState.PendingRemoval;
                }
            }
            else if (_targetThing.Spawned)
            {
                Position = CalculatePositionWhenSpawnedOnMap();
            }
            else
            {
                Position = Vector3.zero;
                State = GraphicObjectState.PendingRemoval;
            }
        }

        private Vector3? CalculatePositionWhenCarriedByPawn(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map != Find.CurrentMap)
            {
                return null;
            }

            Vector3 carrierOffset = GetCarrierDisplayOffset(pawn.Rotation, pawn.DevelopmentalStage);

            float finalX = pawn.DrawPos.x + carrierOffset.x;
            float finalZ = pawn.DrawPos.z + carrierOffset.z;
            float baseOriginalItemY = pawn.DrawPos.y;
            float targetY;

            if (pawn.Rotation == Rot4.North)
            {
                baseOriginalItemY -= VanillaCarriedYOffsetValue;
                targetY = baseOriginalItemY;
            }
            else
            {
                baseOriginalItemY += VanillaCarriedYOffsetValue;
                targetY = baseOriginalItemY + Altitudes.AltInc;
            }

            Vector3 itemCenterPos = new Vector3(finalX, targetY, finalZ);
            Vector3 cornerOffset = CalculateOverlayCornerOffset(_cachedThingDrawSize);
            Vector3 overlayPos = itemCenterPos + cornerOffset;
            overlayPos.y += Altitudes.AltInc * 2;

            return overlayPos;
        }

        private Vector3 GetCarrierDisplayOffset(
            Rot4 pawnRotation,
            DevelopmentalStage developmentalStage
        )
        {
            Vector3 offset = BaseCarriedOffset;
            offset = pawnRotation.AsInt switch
            {
                0 => offset with { z = BaseCarriedNorthZOffset },
                1 => offset with { x = BaseCarriedEastXOffset },
                3 => offset with { x = BaseCarriedWestXOffset },
                _ => offset,
            };

            if (developmentalStage == DevelopmentalStage.Child)
            {
                offset.z += ChildOffsetZ;
            }
            return offset;
        }

        private Vector3 CalculatePositionWhenSpawnedOnMap()
        {
            Vector3 baseDrawPos = _targetThing.DrawPos;
            baseDrawPos.y += Altitudes.AltInc * 2;
            Vector3 cornerOffset = CalculateOverlayCornerOffset(_cachedThingDrawSize);
            return baseDrawPos + cornerOffset;
        }

        private Vector3 CalculateOverlayCornerOffset(Vector2 thingDrawSize)
        {
            return new Vector3(
                thingDrawSize.x / 2f - overlayGraphicSize / 2f,
                0f,
                thingDrawSize.y / 2f - overlayGraphicSize / 2f
            );
        }

        public void Render()
        {
            if (State != GraphicObjectState.Active || Position == Vector3.zero)
            {
                return;
            }

            if (string.IsNullOrEmpty(_currentTexturePath))
            {
                if (_cachedMaterial != null)
                {
                    _cachedMaterial = null;
                }
                _materialTexturePathUsedForCache = null;
                return;
            }

            if (_cachedMaterial == null || _materialTexturePathUsedForCache != _currentTexturePath)
            {
                _cachedMaterial = MaterialPool.MatFrom(
                    _currentTexturePath,
                    ShaderDatabase.MetaOverlay
                );
                _materialTexturePathUsedForCache = _currentTexturePath;
            }

            if (_cachedMaterial == null)
            {
                return;
            }

            Mesh mesh = MeshPool.plane10;
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(overlayGraphicSize, 1f, overlayGraphicSize);

            Vector3 finalDrawPos = Position;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            Color color = Color.white;
            color.a = Alpha;
            mpb.SetColor(ShaderPropertyIDs.Color, color);

            Matrix4x4 matrix = Matrix4x4.TRS(finalDrawPos, rotation, scale);

            UnityEngine.Graphics.DrawMesh(mesh, matrix, _cachedMaterial, 0, null, 0, mpb);
        }

        private static bool IsValid(Thing thing) =>
            thing != null && !thing.Destroyed && thing.SpawnedOrAnyParentSpawned;

        public void Dispose()
        {
            _cachedMaterial = null;
            _materialTexturePathUsedForCache = null;
        }
    }
}
