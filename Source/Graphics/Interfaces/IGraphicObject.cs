using System;
using Verse;

namespace PressR.Graphics.Interfaces
{
    public enum GraphicObjectState
    {
        Active,
        PendingRemoval,
    }

    public interface IGraphicObject : IIdentifiable<object>, IDisposable
    {
        GraphicObjectState State { get; set; }

        void Update();

        void Render();
    }
}
