namespace PressR.Features
{
    public interface IPressRFeature
    {
        string FeatureId { get; }
        string Label { get; }
        bool IsActive { get; }

        void ConstantUpdate();
        bool TryActivate();
        void Deactivate();
        void Update();
    }
}
