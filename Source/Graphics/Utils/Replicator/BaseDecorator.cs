using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public abstract class BaseDecorator : IRenderDataReplicatorDecorator
    {
        public abstract string GetDecoratorName();

        public virtual bool IsEnabled()
        {
            return true;
        }

        public abstract bool CanApply(Thing thing);

        public virtual ThingRenderData Decorate(ThingRenderData renderData, Thing thing)
        {
            return renderData;
        }

        protected bool IsInStorage(Thing thing)
        {
            if (thing?.Map == null)
                return false;
            Building edifice = thing.Position.GetEdifice(thing.Map);
            return edifice != null && edifice is Building_Storage;
        }

        protected bool IsEquipped(Thing thing)
        {
            if (thing is ThingWithComps thingWithComps)
            {
                return thingWithComps.ParentHolder is Pawn_EquipmentTracker;
            }
            return false;
        }

        protected bool IsCarried(Thing thing)
        {
            return thing.ParentHolder is Pawn_CarryTracker;
        }
    }
}
