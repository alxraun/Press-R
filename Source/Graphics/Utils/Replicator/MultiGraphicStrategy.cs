using LudeonTK;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class MultiGraphicStrategy : BaseRenderStrategy
    {
#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableMultiGraphicStrategy = true;
#endif

        public override string GetStrategyName() => "MultiGraphicStrategy";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableMultiGraphicStrategy;
#else
            return true;
#endif
        }

        public override bool CanHandle(Thing thing)
        {
            return base.CanHandle(thing) && thing.Graphic is Graphic_Multi;
        }

        public override Vector3 GetScale(Thing thing)
        {
            Vector3 scale = base.GetScale(thing);

            if (thing.Rotation.IsHorizontal)
            {
                float tempX = scale.x;
                scale.x = scale.z;
                scale.z = tempX;
            }

            return scale;
        }
    }
}
