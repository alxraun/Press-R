using System;
using PressR.Features.DirectHaul.Core;
using PressR.Utils.Throttler;
using RimWorld;
using Verse;

namespace PressR
{
    public class PressRMapComponent : MapComponent
    {
        private DirectHaulExposableData _directHaulExposableData;
        private readonly ThrottledValue<bool> _drawingMapCache;

        public DirectHaulExposableData DirectHaulExposableData => _directHaulExposableData;

        public PressRMapComponent(Map map)
            : base(map)
        {
            _directHaulExposableData = new DirectHaulExposableData(map);
            _drawingMapCache = new ThrottledValue<bool>(
                1,
                () => RimWorld.Planet.WorldRendererUtility.DrawingMap
            );
            PressRMain.GraphicsManager?.Clear();
        }

        public override void MapComponentOnGUI()
        {
            PressRMain.MainUpdateLoop();
        }

        public override void MapComponentUpdate()
        {
            if (!_drawingMapCache.GetValue())
            {
                return;
            }

            PressRMain.GraphicsManager?.UpdateTweens();
            PressRMain.GraphicsManager?.UpdateGraphicObjects();
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
