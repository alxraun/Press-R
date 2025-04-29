using System.Collections.Generic;
using Verse;

namespace PressR.Features.TabLens
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
