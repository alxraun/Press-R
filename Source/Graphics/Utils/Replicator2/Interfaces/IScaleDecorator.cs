using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IScaleDecorator : IRenderDataDecorator
    {
        Vector3 ModifyScale(RenderContext context, Vector3 currentScale);
    }
}
