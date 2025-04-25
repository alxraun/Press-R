using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Core
{
    public abstract class BaseDecorator : IRenderDataDecorator
    {
        public abstract int Priority { get; }

        public virtual bool IsEnabled() => true;

        public abstract bool CanApply(RenderContext context);

        protected bool IsInStorage(Thing thing)
        {
            if (thing?.Map == null || !thing.Spawned)
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
