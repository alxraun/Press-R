using System;
using UnityEngine;
using Verse;

namespace PressR.Graphics.GraphicObjects
{
    public class ThingRenderStateCache
    {
        private readonly Thing _trackedThing;
        private readonly Func<Thing, ThingRenderStateCache, bool> _isUpdateRequiredPredicate;

        public int LastUpdateTick { get; private set; } = -1;
        public IntVec3 LastPosition { get; private set; }
        public Rot4 LastRotation { get; private set; }
        public int LastMapId { get; private set; } = -1;
        public int LastStackCount { get; private set; } = -1;
        public ThingDef LastStuffDef { get; private set; } = null;
        public IThingHolder LastParentHolder { get; private set; } = null;
        public Vector3 LastDrawPos { get; private set; }
        public Graphic LastGraphic { get; private set; }

        public Pawn LastCarrierPawn { get; private set; } = null;
        public Vector3 LastCarrierDrawPos { get; private set; }
        public IntVec3 LastCarrierPosition { get; private set; }
        public Rot4 LastCarrierRotation { get; private set; }

        public ThingRenderStateCache(
            Thing thingToTrack,
            Func<Thing, ThingRenderStateCache, bool> isUpdateRequiredPredicate
        )
        {
            _trackedThing = thingToTrack ?? throw new ArgumentNullException(nameof(thingToTrack));
            _isUpdateRequiredPredicate =
                isUpdateRequiredPredicate
                ?? throw new ArgumentNullException(nameof(isUpdateRequiredPredicate));
        }

        public bool IsUpdateNeeded()
        {
            if (_trackedThing.DestroyedOrNull())
            {
                return _isUpdateRequiredPredicate(_trackedThing, this);
            }
            return _isUpdateRequiredPredicate(_trackedThing, this);
        }

        public void RecordCurrentState()
        {
            if (_trackedThing.DestroyedOrNull())
                return;

            LastUpdateTick = GenTicks.TicksGame;
            LastPosition = _trackedThing.Position;
            LastRotation = _trackedThing.Rotation;
            LastMapId = _trackedThing.Map?.uniqueID ?? -1;
            LastStackCount = _trackedThing.stackCount;
            LastStuffDef = _trackedThing.Stuff;
            LastParentHolder = _trackedThing.ParentHolder;
            LastDrawPos = _trackedThing.DrawPos;
            LastGraphic = _trackedThing.Graphic;

            Pawn carrierPawn = (LastParentHolder as Pawn_CarryTracker)?.pawn;
            LastCarrierPawn = carrierPawn;
            if (carrierPawn != null)
            {
                LastCarrierDrawPos = carrierPawn.DrawPos;
                LastCarrierPosition = carrierPawn.Position;
                LastCarrierRotation = carrierPawn.Rotation;
            }
            else
            {
                LastCarrierDrawPos = Vector3.zero;
                LastCarrierPosition = IntVec3.Invalid;
                LastCarrierRotation = Rot4.Invalid;
            }
        }
    }
}
