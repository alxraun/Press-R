using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IMaterialDecorator : IRenderDataDecorator
    {
        Material ModifyMaterial(RenderContext context, Material currentMaterial);
    }
}
