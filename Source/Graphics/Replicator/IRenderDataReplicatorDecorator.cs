using Verse;

namespace PressR.Graphics.Replicator
{
    public interface IRenderDataReplicatorDecorator
    {
        bool CanApply(Thing thing);
        ThingRenderData Decorate(ThingRenderData renderData, Thing thing);
        string GetDecoratorName();
        bool IsEnabled();
    }
}
