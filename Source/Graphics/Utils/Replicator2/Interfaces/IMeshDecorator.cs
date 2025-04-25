using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IMeshDecorator : IRenderDataDecorator
    {
        Mesh ModifyMesh(RenderContext context, Mesh currentMesh);
    }
}
