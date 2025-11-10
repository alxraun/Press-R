using System;

namespace Microtools.Graphics
{
    public enum GraphicObjectState
    {
        Active,
        PendingRemoval,
    }

    public interface IGraphicObject : IIdentifiable<object>, IDisposable
    {
        GraphicObjectState State { get; set; }

        void OnRegistered();

        void Update();

        void Render();
    }
}
