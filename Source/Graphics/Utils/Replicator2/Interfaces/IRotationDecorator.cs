using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IRotationDecorator : IRenderDataDecorator
    {
        Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation);
    }
}
