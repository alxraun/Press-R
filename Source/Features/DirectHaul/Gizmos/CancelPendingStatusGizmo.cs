using PressR.Features.DirectHaul.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul.Gizmos
{
    public class CancelPendingStatusGizmo : Command_Action
    {
        private readonly Thing _pendingThing;
        private readonly DirectHaulExposableData _directHaulData;

        public CancelPendingStatusGizmo(Thing pendingThing, DirectHaulExposableData directHaulData)
        {
            _pendingThing = pendingThing;
            _directHaulData = directHaulData;

            defaultLabel = "PressR.DirectHaul.CancelPendingStatusGizmo.Label".Translate();
            defaultDesc = "PressR.DirectHaul.CancelPendingStatusGizmo.Desc".Translate();

            icon = ContentFinder<Texture2D>.Get("cancel_pending_status_gizmo", true);
            Order = -99f;

            action = () =>
            {
                _directHaulData?.RemoveThingFromTracking(_pendingThing);
                SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
            };
        }
    }
}
