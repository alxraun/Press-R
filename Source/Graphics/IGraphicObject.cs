using System;

namespace PressR.Graphics
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
