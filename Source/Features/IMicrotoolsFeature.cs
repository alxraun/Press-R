using Verse;

namespace Microtools.Features
{
    public interface IMicrotoolsFeature : IExposable
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
