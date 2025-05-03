using System.Collections.Generic;
using PressR.Debugger;
using PressR.Graphics;
using Verse;
using static PressR.Debugger.DebuggerTrackedValueInfo;

namespace PressR.Debugger.Configs
{
    public class GraphicsManagerConfig : IDebuggerConfig
    {
        public string Name => "Graphics Manager State";

        public float UpdateInterval => 0.1f;

        public float StartDelaySeconds => 0f;

        public IEnumerable<DebuggerTrackedValueInfo> GetTrackedValues()
        {
            return new List<DebuggerTrackedValueInfo>
            {
                TrackCollectionCount(
                    "PressR.PressRMain._graphicsManager._graphicObjects",
                    "Graphic Objects"
                ),
                TrackCollectionCount(
                    "PressR.PressRMain._graphicsManager._activeEffects",
                    "Active Effects"
                ),
                TrackCollectionCount(
                    "PressR.PressRMain._graphicsManager._objectToActiveEffectIds",
                    "GO to AE IDs"
                ),
            };
        }
    }
}
