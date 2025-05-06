using System;

namespace PressR.Graphics.Tween
{
    public interface ITween : IIdentifiable<Guid>
    {
        string PropertyId { get; }
        bool IsFinished { get; }

        Action OnComplete { get; set; }

        void Update(float deltaTime);

        void Complete();

        void Kill();
    }
}
