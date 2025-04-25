using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IPositionOffsetDecorator : IRenderDataDecorator
    {
        Vector3 GetPositionOffsetDelta(RenderContext context);
    }
}
