using PressR.Features.DirectHaul.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PressR.Features.DirectHaul.Gizmos
{
    public class CancelHeldStatusGizmo : Command_Action
    {
        private readonly Thing _heldThing;
        private readonly DirectHaulExposableData _directHaulData;

        public CancelHeldStatusGizmo(Thing heldThing, DirectHaulExposableData directHaulData)
        {
            _heldThing = heldThing;
            _directHaulData = directHaulData;

            defaultLabel = "PressR.DirectHaul.CancelHeldStatusGizmo.Label".Translate();
            defaultDesc = "PressR.DirectHaul.CancelHeldStatusGizmo.Desc".Translate();

            icon = ContentFinder<Texture2D>.Get("DirectHaul/cancel_held_status_gizmo", true);
            Order = -100f;

            action = () =>
            {
                _directHaulData?.RemoveThingFromTracking(_heldThing);
                SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
            };
        }
    }
}
