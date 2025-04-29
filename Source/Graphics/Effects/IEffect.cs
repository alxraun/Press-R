using System;
using System.Collections.Generic;
using PressR.Graphics.GraphicObjects;

namespace PressR.Graphics.Effects
{
    public enum EffectState
    {
        Active,
        PendingRemoval,
    }

    public interface IEffect : IIdentifiable<Guid>
    {
        List<IGraphicObject> Targets { get; }

        EffectState State { get; set; }

        bool IsFinished { get; }

        void Update(float deltaTime);

        void OnAttach(IGraphicObject target);

        void OnDetach(IGraphicObject target);
    }
}
