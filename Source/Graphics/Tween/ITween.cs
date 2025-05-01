using System;

namespace PressR.Graphics.Tween
{
    public interface ITween : IIdentifiable<Guid>
    {
        bool IsFinished { get; }

        Action OnComplete { get; set; }

        void Update(float deltaTime);

        void Complete();

        void Kill();
    }
}
