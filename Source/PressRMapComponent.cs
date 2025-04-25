using PressR.Features.DirectHaul.Core;
using Verse;

namespace PressR
{
    public class PressRMapComponent : MapComponent
    {
        private DirectHaulExposableData _directHaulExposableData;

        public DirectHaulExposableData DirectHaulExposableData => _directHaulExposableData;

        public PressRMapComponent(Map map)
            : base(map)
        {
            _directHaulExposableData = new DirectHaulExposableData(map);
            PressRMain.GraphicsManager?.Clear();
        }

        public override void MapComponentOnGUI()
        {
            PressRMain.MainUpdateLoop();
        }

        public override void MapComponentUpdate()
        {
            PressRMain.GraphicsManager?.Update();
            PressRMain.GraphicsManager?.RenderGraphicObjects();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref _directHaulExposableData, "directHaulData", this.map);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _directHaulExposableData == null)
            {
                _directHaulExposableData = new DirectHaulExposableData(this.map);
            }
        }
    }
}
