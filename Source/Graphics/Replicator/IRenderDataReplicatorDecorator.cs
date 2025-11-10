using Verse;

namespace Microtools.Graphics.Replicator
{
    public interface IRenderDataReplicatorDecorator
    {
        bool CanApply(Thing thing);
        ThingRenderData Decorate(ThingRenderData renderData, Thing thing);
        string GetDecoratorName();
        bool IsEnabled();
    }
}
