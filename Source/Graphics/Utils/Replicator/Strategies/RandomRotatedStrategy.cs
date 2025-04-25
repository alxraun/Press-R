using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class RandomRotatedStrategy : BaseRenderStrategy
    {
#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableRandomRotatedStrategy = true;
#endif

        private const float DefaultMaxRandomAngle = 35f;
        private const float RandomAngleMultiplier = 542f;
        private const float ShelfRotationAngle = -90f;

        public override string GetStrategyName() => "RandomRotatedStrategy";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableRandomRotatedStrategy;
#else
            return true;
#endif
        }

        public override bool CanHandle(Thing thing)
        {
            return base.CanHandle(thing) && thing.Graphic is Graphic_RandomRotated;
        }

        public override Quaternion GetRotation(Thing thing, Rot4 rot, float extraRotation)
        {
            Quaternion quat = base.GetRotation(thing, rot, 0f);

            if (thing.def.rotateInShelves && IsInStorage(thing))
            {
                return Quaternion.AngleAxis(ShelfRotationAngle, Vector3.up);
            }

            float randomRot = ReplicatorHelper.GetRandomRotationAngle(
                thing.Graphic,
                thing,
                DefaultMaxRandomAngle,
                RandomAngleMultiplier
            );

            quat *= Quaternion.Euler(Vector3.up * (randomRot + extraRotation));

            return quat;
        }
    }
}
