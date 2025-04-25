using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Interfaces
{
    public interface IRenderDataReplicatorDecorator
    {
        bool CanApply(Thing thing);
        ThingRenderData Decorate(ThingRenderData renderData, Thing thing);
        string GetDecoratorName();
        bool IsEnabled();
    }
}
