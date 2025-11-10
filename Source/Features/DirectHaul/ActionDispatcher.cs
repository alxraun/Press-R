using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Microtools.Features.DirectHaul
{
    public class ActionDispatcher(ThingStateManager thingStateManager, State state)
    {
        private readonly State _state = state;
        private readonly ThingStateManager _thingStateManager = thingStateManager;

        public void PlaceItems()
        {
            if (_state.GhostPlacements.Count == 0)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            foreach (var placement in _state.GhostPlacements)
            {
                var thing = placement.Key;
                var cell = placement.Value;
                _thingStateManager.SetPending(thing, new LocalTargetInfo(cell));
            }
            SoundDefOf.Designate_Haul.PlayOneShotOnCamera();
        }

        public void CancelPlacements(IEnumerable<Thing> things)
        {
            var thingsToCancel = things.ToList();
            if (thingsToCancel.Count == 0)
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                return;
            }

            foreach (var thing in thingsToCancel)
            {
                if (_thingStateManager.GetStatus(thing) != DirectHaulStatus.None)
                {
                    _thingStateManager.Remove(thing);
                }
            }
            SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
        }
    }
}
