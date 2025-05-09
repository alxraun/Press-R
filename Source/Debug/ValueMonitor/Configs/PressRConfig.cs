using System.Collections.Generic;
using PressR.Debug.ValueMonitor;
using PressR.Graphics;
using Verse;
using static PressR.Debug.ValueMonitor.ValueMonitorTrackedValueInfo;

namespace PressR.Debug.ValueMonitor.Configs
{
    public class PressRConfig : IValueMonitorConfig
    {
        public string Name => "PressR Config";

        public float UpdateInterval => 0.05f;

        public float StartDelaySeconds => 3f;

        public IEnumerable<ValueMonitorTrackedValueInfo> GetTrackedValues()
        {
            return new List<ValueMonitorTrackedValueInfo>
            {
                TrackValue(
                    "PressR.PressRInput.IsPressRModifierKeyPressed",
                    "IsPressRModifierKeyPressed"
                ),
                TrackValue("PressR.PressRInput.IsMouseButtonHeld", "IsMouseButtonHeld"),
                TrackCollectionCount(
                    "PressR.PressRMain._graphicsManager._graphicObjects",
                    "Graphic Objects"
                ),
                TrackCollectionCount(
                    "PressR.PressRMain._graphicsManager._activeTweens",
                    "Active Tweens"
                ),
            };
        }
    }
}
