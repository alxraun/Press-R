using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Microtools.Features.DirectHaul
{
    public class Command_CancelHeldStatus : Command_Action
    {
        private readonly Thing _heldThing;
        private readonly ThingStateManager _thingStateManager;

        public Command_CancelHeldStatus(Thing heldThing, ThingStateManager thingStateManager)
        {
            _heldThing = heldThing;
            _thingStateManager = thingStateManager;

            defaultLabel = "Microtools.DirectHaul.Command_CancelHeldStatus.Label".Translate();
            defaultDesc = "Microtools.DirectHaul.Command_CancelHeldStatus.Desc".Translate();

            icon = ContentFinder<Texture2D>.Get("dh_cancel_held_status_gizmo", true);
            Order = -100f;
            hotKey = KeyBindingDefOf.Designator_Cancel;

            action = () =>
            {
                _thingStateManager.Remove(_heldThing);
                SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
            };
        }

        public override void ProcessInput(Event ev)
        {
            if (ev.button == 1)
            {
                var options = new List<FloatMenuOption>
                {
                    new(
                        "Microtools.DirectHaul.Command_CancelHeldStatus.CancelAllInView".Translate(),
                        () => RemoveAllInView()
                    ),
                    new(
                        "Microtools.DirectHaul.Command_CancelHeldStatus.CancelAllOnMap".Translate(),
                        () => RemoveAllOnMap()
                    ),
                };

                Find.WindowStack.Add(new FloatMenu(options));
            }
            else
            {
                base.ProcessInput(ev);
            }
        }

        private void RemoveAllInView()
        {
            var thingsToRemove = new List<Thing>();
            foreach (var thing in _thingStateManager.AllHeldThings)
            {
                if (
                    thing.Map == Find.CurrentMap
                    && Find.CameraDriver.CurrentViewRect.Contains(thing.Position)
                )
                {
                    thingsToRemove.Add(thing);
                }
            }
            _thingStateManager.Remove(thingsToRemove);
            SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
        }

        private void RemoveAllOnMap()
        {
            var thingsToRemove = new List<Thing>();
            foreach (var thing in _thingStateManager.AllHeldThings)
            {
                if (thing.Map == Find.CurrentMap)
                {
                    thingsToRemove.Add(thing);
                }
            }
            _thingStateManager.Remove(thingsToRemove);
            SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
        }
    }
}
