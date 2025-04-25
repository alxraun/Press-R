using LudeonTK;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class SingleGraphicStrategy : BaseRenderStrategy
    {
        private const float ShelfRotationAngle = -90f;

        private const float MultiItemScaleFactor = 1.0f;

#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableSingleGraphicStrategy = true;
#endif

        public override string GetStrategyName() => "SingleGraphicStrategy";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableSingleGraphicStrategy;
#else
            return true;
#endif
        }

        public override bool CanHandle(Thing thing)
        {
            return base.CanHandle(thing) && thing.Graphic is Graphic_Single;
        }

        public override Quaternion GetRotation(Thing thing, Rot4 rot, float extraRotation)
        {
            Quaternion quat = base.GetRotation(thing, rot, extraRotation);

            if (thing.def.rotateInShelves && IsInStorage(thing))
            {
                quat = Quaternion.AngleAxis(ShelfRotationAngle, Vector3.up);
            }

            return quat;
        }

        public override Vector3 GetScale(Thing thing)
        {
            Vector3 scale = base.GetScale(thing);

            if (HasMultipleItemsInSameCell(thing))
            {
                scale *= MultiItemScaleFactor;
            }

            return scale;
        }
    }
}
