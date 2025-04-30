using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public abstract class BaseRenderStrategy : IRenderDataReplicatorStrategy
    {
#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableBaseRenderStrategy = true;
#endif

        public virtual bool CanHandle(Thing thing)
        {
            return thing != null && thing.Graphic != null;
        }

        public virtual Mesh GetMesh(Thing thing, Rot4 rot)
        {
            return thing.Graphic.MeshAt(rot);
        }

        public virtual Material GetMaterial(Thing thing, Rot4 rot)
        {
            Material originalMaterial = thing.Graphic.MatSingleFor(thing);
            return originalMaterial;
        }

        public virtual Quaternion GetRotation(Thing thing, Rot4 rot, float extraRotation)
        {
            Quaternion quat = Quaternion.AngleAxis(rot.AsAngle, Vector3.up);

            if (extraRotation != 0f)
            {
                quat *= Quaternion.Euler(Vector3.up * extraRotation);
            }

            return quat;
        }

        public virtual Vector3 GetPositionOffset(Thing thing, Rot4 rot)
        {
            return thing.Graphic.DrawOffset(rot);
        }

        public virtual Vector3 GetScale(Thing thing)
        {
            return Vector3.one;
        }

        public virtual string GetStrategyName() => "BaseRenderStrategy";

        public virtual bool IsEnabled()
        {
#if DEBUG
            return EnableBaseRenderStrategy;
#else
            return true;
#endif
        }

        protected bool IsInStorage(Thing thing)
        {
            if (thing?.Map == null || !thing.Spawned)
                return false;
            Building edifice = thing.Position.GetEdifice(thing.Map);
            return edifice != null && edifice is Building_Storage;
        }

        protected bool HasMultipleItemsInSameCell(Thing thing)
        {
            if (thing?.Map == null || !thing.Spawned)
                return false;
            return thing
                    .Map.thingGrid.ThingsListAtFast(thing.Position)
                    .Count(t => t != thing && t.def.category == ThingCategory.Item) > 0;
        }
    }
}
