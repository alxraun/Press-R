using PressR.Graphics.Utils.Replicator2.Core;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IRenderDataDecorator
    {
        int Priority { get; }

        bool IsEnabled();

        bool CanApply(RenderContext context);
    }
}
