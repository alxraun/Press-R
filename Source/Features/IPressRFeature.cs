using Verse;

namespace PressR.Features
{
    public interface IPressRFeature : IExposable
    {
        bool IsActive { get; }
        void Update();
        virtual void ConstantUpdate() { }
        virtual void ConstantClear() { }
        bool CanActivate();
        void Activate();
        void Deactivate();
    }
}
