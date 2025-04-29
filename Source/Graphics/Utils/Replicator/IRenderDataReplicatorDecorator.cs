using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public interface IRenderDataReplicatorDecorator
    {
        bool CanApply(Thing thing);
        ThingRenderData Decorate(ThingRenderData renderData, Thing thing);
        string GetDecoratorName();
        bool IsEnabled();
    }
}
