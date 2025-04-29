using System.Collections.Generic;
using PressR.Graphics;
using Verse;

namespace PressR.Interfaces
{
    public interface ILens
    {
        string LensId { get; }
        string Label { get; }
        bool IsActive { get; }
        bool TryActivate();
        void Deactivate();
        void Update();
    }
}
