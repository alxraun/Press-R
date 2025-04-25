using System;
using PressR.Graphics.Interfaces;
using PressR.Graphics.Shaders;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul.Graphics.GraphicObjects
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

        public DirectHaulStatusOverlayGraphicObject(Thing targetThing)
        {
            _targetThing =
                targetThing ?? throw new System.ArgumentNullException(nameof(targetThing));
        }

        public void UpdateVisualState(string texturePath)
        {
            _currentTexturePath = texturePath;
        }

        public void Update()
        {
            if (!IsValid(_targetThing))
            {
                State = GraphicObjectState.PendingRemoval;
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
                Pawn pawn = carryTracker.pawn;
                if (!pawn.Spawned || pawn.Map != Find.CurrentMap)
                {
                    Position = Vector3.zero;
                    State = GraphicObjectState.PendingRemoval;
                    return;
                }

                Vector3 currentOffset = BaseCarriedOffset;
                Rot4 pawnRotation = pawn.Rotation;
                switch (pawnRotation.AsInt)
                {
                    case 0:
                        currentOffset.z = BaseCarriedNorthZOffset;
                        break;
                    case 1:
                        currentOffset.x = BaseCarriedEastXOffset;
                        break;
                    case 3:
                        currentOffset.x = BaseCarriedWestXOffset;
                        break;
                }

                if (pawn.DevelopmentalStage == DevelopmentalStage.Child)
                {
                    currentOffset.z += ChildOffsetZ;
                }

                float finalX = pawn.DrawPos.x + currentOffset.x;
                float finalZ = pawn.DrawPos.z + currentOffset.z;

                float baseOriginalItemY = pawn.DrawPos.y;
                float targetY;

                if (pawnRotation == Rot4.North)
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

                Vector2 thingDrawSize = _targetThing.Graphic.drawSize;
                Vector3 cornerOffset = new Vector3(
                    thingDrawSize.x / 2f - overlayGraphicSize / 2f,
                    0f,
                    thingDrawSize.y / 2f - overlayGraphicSize / 2f
                );

                Vector3 overlayPos = itemCenterPos + cornerOffset;

                overlayPos.y += Altitudes.AltInc * 2;

                Position = overlayPos;
            }
            else if (_targetThing.Spawned)
            {
                Vector3 baseDrawPos = _targetThing.DrawPos;
                baseDrawPos.y += Altitudes.AltInc * 2;

                Vector2 thingDrawSize = _targetThing.Graphic.drawSize;
                Vector3 cornerOffset = new Vector3(
                    thingDrawSize.x / 2f - overlayGraphicSize / 2f,
                    0f,
                    thingDrawSize.y / 2f - overlayGraphicSize / 2f
                );

                Position = baseDrawPos + cornerOffset;
            }
            else
            {
                Position = Vector3.zero;
                State = GraphicObjectState.PendingRemoval;
            }
        }

        public void Render()
        {
            if (
                State != GraphicObjectState.Active
                || string.IsNullOrEmpty(_currentTexturePath)
                || Position == Vector3.zero
            )
            {
                return;
            }

            Material material = MaterialPool.MatFrom(
                _currentTexturePath,
                ShaderDatabase.MetaOverlay
            );

            if (material == null)
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

            UnityEngine.Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, mpb);
        }

        private static bool IsValid(Thing thing) =>
            thing != null && !thing.Destroyed && thing.SpawnedOrAnyParentSpawned;

        public void Dispose() { }
    }
}
