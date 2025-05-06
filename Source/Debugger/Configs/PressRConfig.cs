using System.Collections.Generic;
using PressR.Debugger;
using PressR.Graphics;
using Verse;
using static PressR.Debugger.DebuggerTrackedValueInfo;

namespace PressR.Debugger.Configs
{
    public class PressRConfig : IDebuggerConfig
    {
        public string Name => "PressR Config";

        public float UpdateInterval => 0.05f;

        public float StartDelaySeconds => 3f;

        public IEnumerable<DebuggerTrackedValueInfo> GetTrackedValues()
        {
            return new List<DebuggerTrackedValueInfo>
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
