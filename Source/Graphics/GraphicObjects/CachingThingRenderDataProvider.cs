using System;
using PressR.Graphics.Utils.Replicator;
using UnityEngine;
using Verse;

namespace PressR.Graphics.GraphicObjects
{
    [Flags]
    public enum TrackedStateParts
    {
        None = 0,
        Position = 1 << 0,
        Rotation = 1 << 1,
        MapId = 1 << 2,
        StackCount = 1 << 3,
        Stuff = 1 << 4,
        ParentHolder = 1 << 5,
        DrawPos = 1 << 6,
        Graphic = 1 << 7,

        Transform = Position | Rotation | DrawPos,
        InventoryItem = StackCount | Stuff,
        All = ~None,
    }

    public class CachingThingRenderDataProvider : IDisposable
    {
        private readonly Thing _trackedThing;
        private readonly TrackedStateParts _partsToTrack;
        private readonly bool _provideClientWithCopy;

        private ThingRenderData _cachedRenderData;
        private Material _actualOriginalMaterialFromReplicator;
        private Material _copiedMaterialForClientCache;
        private bool _disposed = false;

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

        public CachingThingRenderDataProvider(
            Thing thingToTrack,
            TrackedStateParts partsToTrack,
            bool provideClientWithCopy = false
        )
        {
            _trackedThing = thingToTrack ?? throw new ArgumentNullException(nameof(thingToTrack));
            _partsToTrack = partsToTrack;
            _provideClientWithCopy = provideClientWithCopy;

            RefreshData();
        }

        private void RefreshData()
        {
            if (!IsTrackedThingValid())
            {
                if (_actualOriginalMaterialFromReplicator != null)
                {
                    _actualOriginalMaterialFromReplicator = null;
                    if (_copiedMaterialForClientCache != null)
                    {
                        UnityEngine.Object.Destroy(_copiedMaterialForClientCache);
                        _copiedMaterialForClientCache = null;
                    }
                }
                _cachedRenderData = new ThingRenderData(null, Matrix4x4.identity, null);
                RecordCurrentState();
                return;
            }

            ThingRenderData replicatedRenderData = ThingRenderDataReplicator.GetRenderData(
                _trackedThing,
                true
            );

            if (_actualOriginalMaterialFromReplicator != replicatedRenderData.Material)
            {
                _actualOriginalMaterialFromReplicator = replicatedRenderData.Material;
                if (_copiedMaterialForClientCache != null)
                {
                    UnityEngine.Object.Destroy(_copiedMaterialForClientCache);
                    _copiedMaterialForClientCache = null;
                }
            }

            Material materialToProvideToClient;
            if (_provideClientWithCopy)
            {
                if (
                    _actualOriginalMaterialFromReplicator != null
                    && _copiedMaterialForClientCache == null
                )
                {
                    _copiedMaterialForClientCache = new Material(
                        _actualOriginalMaterialFromReplicator
                    );
                }
                materialToProvideToClient = _copiedMaterialForClientCache;
            }
            else
            {
                materialToProvideToClient = _actualOriginalMaterialFromReplicator;
            }

            _cachedRenderData = new ThingRenderData(
                replicatedRenderData.Mesh,
                replicatedRenderData.Matrix,
                materialToProvideToClient
            );

            RecordCurrentState();
        }

        public ThingRenderData GetRenderData()
        {
            if (!IsTrackedThingValid())
            {
                if (
                    _cachedRenderData.Mesh != null
                    || _cachedRenderData.Material != null
                    || LastMapId != -1
                )
                {
                    RefreshData();
                }
                return _cachedRenderData;
            }

            if (IsUpdateRequired())
            {
                RefreshData();
            }
            return _cachedRenderData;
        }

        public void RecordCurrentState()
        {
            LastUpdateTick = GenTicks.TicksGame;

            if (!IsTrackedThingValid())
            {
                if ((_partsToTrack & TrackedStateParts.Position) != 0)
                    LastPosition = IntVec3.Invalid;
                if ((_partsToTrack & TrackedStateParts.Rotation) != 0)
                    LastRotation = Rot4.Invalid;
                if ((_partsToTrack & TrackedStateParts.MapId) != 0)
                    LastMapId = -1;
                if ((_partsToTrack & TrackedStateParts.StackCount) != 0)
                    LastStackCount = 0;
                if ((_partsToTrack & TrackedStateParts.Stuff) != 0)
                    LastStuffDef = null;
                if ((_partsToTrack & TrackedStateParts.DrawPos) != 0)
                    LastDrawPos = Vector3.zero;
                if ((_partsToTrack & TrackedStateParts.Graphic) != 0)
                    LastGraphic = null;
                if ((_partsToTrack & TrackedStateParts.ParentHolder) != 0)
                {
                    LastParentHolder = null;
                    LastCarrierPawn = null;
                    LastCarrierDrawPos = Vector3.zero;
                    LastCarrierPosition = IntVec3.Invalid;
                    LastCarrierRotation = Rot4.Invalid;
                }
                return;
            }

            if ((_partsToTrack & TrackedStateParts.Position) != 0)
                LastPosition = _trackedThing.Position;
            if ((_partsToTrack & TrackedStateParts.Rotation) != 0)
                LastRotation = _trackedThing.Rotation;
            if ((_partsToTrack & TrackedStateParts.MapId) != 0)
                LastMapId = _trackedThing.Map?.uniqueID ?? -1;
            if ((_partsToTrack & TrackedStateParts.StackCount) != 0)
                LastStackCount = _trackedThing.stackCount;
            if ((_partsToTrack & TrackedStateParts.Stuff) != 0)
                LastStuffDef = _trackedThing.Stuff;
            if ((_partsToTrack & TrackedStateParts.DrawPos) != 0)
                LastDrawPos = _trackedThing.DrawPos;
            if ((_partsToTrack & TrackedStateParts.Graphic) != 0)
                LastGraphic = _trackedThing.Graphic;

            if ((_partsToTrack & TrackedStateParts.ParentHolder) != 0)
            {
                LastParentHolder = _trackedThing.ParentHolder;
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

        public bool IsTrackedThingValid()
        {
            return _trackedThing != null && !_trackedThing.Destroyed;
        }

        private bool IsUpdateRequired()
        {
            if (LastUpdateTick == -1)
                return true;
            if (!IsTrackedThingValid())
            {
                return _cachedRenderData.Mesh != null || _cachedRenderData.Material != null;
            }

            if (
                (_partsToTrack & TrackedStateParts.Position) != 0
                && _trackedThing.Position != LastPosition
            )
                return true;
            if (
                (_partsToTrack & TrackedStateParts.Rotation) != 0
                && _trackedThing.Rotation != LastRotation
            )
                return true;
            if (
                (_partsToTrack & TrackedStateParts.MapId) != 0
                && (_trackedThing.Map?.uniqueID ?? -1) != LastMapId
            )
                return true;
            if (
                (_partsToTrack & TrackedStateParts.StackCount) != 0
                && _trackedThing.stackCount != LastStackCount
            )
                return true;
            if (
                (_partsToTrack & TrackedStateParts.Stuff) != 0
                && _trackedThing.Stuff != LastStuffDef
            )
                return true;
            if (
                (_partsToTrack & TrackedStateParts.DrawPos) != 0
                && _trackedThing.DrawPos != LastDrawPos
            )
                return true;
            if (
                (_partsToTrack & TrackedStateParts.Graphic) != 0
                && _trackedThing.Graphic != LastGraphic
            )
                return true;

            if ((_partsToTrack & TrackedStateParts.ParentHolder) != 0)
            {
                IThingHolder currentParentHolder = _trackedThing.ParentHolder;
                if (currentParentHolder != LastParentHolder)
                    return true;

                if (currentParentHolder is Pawn_CarryTracker)
                {
                    Pawn currentCarrierPawn = ((Pawn_CarryTracker)currentParentHolder).pawn;
                    if (currentCarrierPawn != LastCarrierPawn)
                        return true;

                    if (currentCarrierPawn != null)
                    {
                        if (currentCarrierPawn.DrawPos != LastCarrierDrawPos)
                            return true;
                    }
                }
                else if (LastParentHolder is Pawn_CarryTracker && currentParentHolder == null)
                {
                    if (LastCarrierPawn != null)
                        return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_copiedMaterialForClientCache != null)
            {
                UnityEngine.Object.Destroy(_copiedMaterialForClientCache);
                _copiedMaterialForClientCache = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
