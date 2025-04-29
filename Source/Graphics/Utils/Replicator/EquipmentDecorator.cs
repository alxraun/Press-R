using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class EquipmentDecorator : BaseDecorator
    {
        private const float EquipmentYOffset = 0.1f;

#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableEquipmentDecorator = true;
#endif

        public override string GetDecoratorName() => "EquipmentDecorator";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableEquipmentDecorator;
#else
            return true;
#endif
        }

        public override bool CanApply(Thing thing)
        {
#if DEBUG
            if (!EnableEquipmentDecorator)
                return false;
#endif
            return IsEquipped(thing);
        }

        public override ThingRenderData Decorate(ThingRenderData renderData, Thing thing)
        {
#if DEBUG
            if (!EnableEquipmentDecorator)
                return renderData;
#endif

            if (thing.ParentHolder is Pawn_EquipmentTracker equipment)
            {
                Pawn pawn = equipment.pawn;
                if (pawn != null)
                {
                    Vector3 pawnPos = pawn.DrawPos;

                    Vector3 equipmentPos = pawnPos;
                    equipmentPos.y += EquipmentYOffset;

                    Quaternion pawnRot = Quaternion.AngleAxis(pawn.Rotation.AsAngle, Vector3.up);

                    Vector3 scale = renderData.Matrix.lossyScale;

                    Matrix4x4 newMatrix = Matrix4x4.TRS(equipmentPos, pawnRot, scale);

                    renderData.Matrix = newMatrix;
                    return renderData;
                }
            }

            return renderData;
        }
    }
}
