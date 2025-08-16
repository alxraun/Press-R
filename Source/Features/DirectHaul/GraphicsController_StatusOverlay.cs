using System.Collections.Generic;
using PressR.Graphics;
using PressR.Graphics.Tween;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul
{
    public sealed class GraphicsController_StatusOverlay(
        IGraphicsManager graphicsManager,
        State state,
        ThingStateManager thingStateManager
    )
    {
        private readonly IGraphicsManager _graphicsManager = graphicsManager;
        private readonly State _state = state;
        private readonly ThingStateManager _thingStateManager = thingStateManager;

        private const string TexPathPendingFull = "DirectHaul/pending_overlay_full";
        private const string TexPathPendingPart = "DirectHaul/pending_overlay_part_0";
        private const string TexPathHeldFull = "DirectHaul/held_overlay_full";
        private const string TexPathHeldPart = "DirectHaul/held_overlay_part_0";

        private const float HoverDistance = 0.25f;
        private const float FadeOutDuration = 0.035f;
        private const float FadeInDuration = 0.035f;
        private const float MinAlpha = 0.25f;
        private const string AlphaPropertyId = nameof(IHasAlpha.Alpha);

        private class ManagedOverlay
        {
            public GraphicObject_StatusOverlay Overlay;
            public DirectHaulStatus Status;
            public bool IsPartial;
            public LocalTargetInfo Target;
        }

        private readonly Dictionary<Thing, ManagedOverlay> _entries = new(256);
        private readonly HashSet<Thing> _currentlyMinAlpha = [];

        private readonly HashSet<IntVec3> _pendingCells = new(256);
        private readonly HashSet<IntVec3> _pendingCellsMultiple = new(64);
        private readonly HashSet<IntVec3> _heldCells = [];

        private bool _lastStorageLensActive;
        private IntVec3 _lastMouseCell = IntVec3.Invalid;
        private Vector3 _lastMouseWorldPos;
        private readonly HashSet<Thing> _hoveredThings = [];

        private bool _needsResync = true;
        private bool _subscribed;
        private bool _wasVisibleInViewRect;

        public void Clear()
        {
            UnsubscribeFromEvents();
            ClearInternal(applyFadeOut: true);
            _needsResync = true;
            _wasVisibleInViewRect = false;
            _lastStorageLensActive = false;
            _lastMouseCell = IntVec3.Invalid;
            _lastMouseWorldPos = default;
        }

        public void Update()
        {
            if (!PressRMod.Settings.directHaulSettings.enableStatusOverlays)
            {
                ClearInternal(applyFadeOut: true);
                return;
            }

            var map = Find.CurrentMap;
            if (map == null)
            {
                ClearInternal(applyFadeOut: true);
                return;
            }

            if (_thingStateManager.AllTrackedThings.Count == 0)
            {
                if (_entries.Count != 0)
                {
                    ClearInternal(applyFadeOut: true);
                }
                return;
            }

            var viewRect = Find.CameraDriver.CurrentViewRect;
            bool anyVisible = HasAnyVisibleTrackedThing(map, viewRect);
            if (!anyVisible)
            {
                if (_entries.Count != 0)
                {
                    ClearInternal(applyFadeOut: true);
                }
                _wasVisibleInViewRect = false;
                return;
            }
            if (!_wasVisibleInViewRect)
            {
                _needsResync = true;
                _wasVisibleInViewRect = true;
            }

            if (_needsResync)
            {
                SyncAllFromState();
                _needsResync = false;
            }

            EnsureSubscribed();
            RebuildHeldCells();
            UpdatePartialsAndTextures();
            UpdateOverlayAlphasByProximity();
        }

        private void EnsureSubscribed()
        {
            if (_subscribed)
            {
                return;
            }
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_subscribed)
            {
                return;
            }
            _thingStateManager.PendingAdded += OnStateChanged;
            _thingStateManager.PendingTargetChanged += OnStateChanged;
            _thingStateManager.PendingRemoved += OnStateChanged;
            _thingStateManager.HeldAdded += OnStateChanged;
            _thingStateManager.HeldRemoved += OnStateChanged;
            _thingStateManager.CachesRecalculated += OnStateChanged;
            _subscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_subscribed)
            {
                return;
            }
            _thingStateManager.PendingAdded -= OnStateChanged;
            _thingStateManager.PendingTargetChanged -= OnStateChanged;
            _thingStateManager.PendingRemoved -= OnStateChanged;
            _thingStateManager.HeldAdded -= OnStateChanged;
            _thingStateManager.HeldRemoved -= OnStateChanged;
            _thingStateManager.CachesRecalculated -= OnStateChanged;
            _subscribed = false;
        }

        private void OnStateChanged()
        {
            MarkNeedsResync();
        }

        private void OnStateChanged(Thing thing)
        {
            MarkNeedsResync();
        }

        private void OnStateChanged(Thing thing, LocalTargetInfo target)
        {
            MarkNeedsResync();
        }

        private void OnStateChanged(
            Thing thing,
            LocalTargetInfo oldTarget,
            LocalTargetInfo newTarget
        )
        {
            MarkNeedsResync();
        }

        private void MarkNeedsResync()
        {
            _needsResync = true;
        }

        private void SyncAllFromState()
        {
            var desiredThings = CollectDesiredThings();
            RemoveOverlaysNotIn(desiredThings);
            RebuildPendingCounts();
            RebuildHeldCells();

            foreach (var thing in desiredThings)
            {
                if (!_state.TrackedThings.TryGetValue(thing, out var info))
                {
                    continue;
                }

                var isPartial = ComputeIsPartial(thing, info.Status, info.TargetCell);
                UpsertOverlay(thing, info.Status, info.TargetCell, isPartial);
            }
        }

        private void RebuildHeldCells()
        {
            _heldCells.Clear();
            foreach (var thing in _thingStateManager.AllHeldThings)
            {
                if (thing == null)
                {
                    continue;
                }
                var heldCell = thing.PositionHeld;
                if (heldCell.IsValid)
                {
                    _heldCells.Add(heldCell);
                }
            }
        }

        private void RebuildPendingCounts()
        {
            _pendingCells.Clear();
            _pendingCellsMultiple.Clear();
            foreach (var thing in _thingStateManager.AllPendingThings)
            {
                if (thing == null)
                {
                    continue;
                }
                if (
                    _state.TrackedThings.TryGetValue(thing, out var info) && info.TargetCell.IsValid
                )
                {
                    var cell = info.TargetCell.Cell;
                    if (!_pendingCells.Add(cell))
                    {
                        _pendingCellsMultiple.Add(cell);
                    }
                }
            }
        }

        private void UpdatePartialsAndTextures()
        {
            foreach (var (thing, entry) in _entries)
            {
                var isPartial = ComputeIsPartial(thing, entry.Status, entry.Target);

                if (entry.IsPartial != isPartial)
                {
                    entry.IsPartial = isPartial;
                    var texturePath = GetOverlayTexturePath(entry.Status, isPartial);
                    if (texturePath != null && entry.Overlay != null)
                    {
                        entry.Overlay.UpdateTexturePath(texturePath);
                    }
                }
            }
        }

        private HashSet<Thing> CollectDesiredThings()
        {
            var desiredThings = new HashSet<Thing>();

            foreach (var thing in _thingStateManager.AllPendingThings)
            {
                if (thing == null)
                {
                    continue;
                }
                desiredThings.Add(thing);
            }

            foreach (var thing in _thingStateManager.AllHeldThings)
            {
                if (thing == null)
                {
                    continue;
                }
                desiredThings.Add(thing);
            }

            return desiredThings;
        }

        private void RemoveOverlaysNotIn(HashSet<Thing> desiredThings)
        {
            var toRemove = new List<Thing>();
            foreach (var kv in _entries)
            {
                if (!desiredThings.Contains(kv.Key))
                {
                    toRemove.Add(kv.Key);
                }
            }
            if (toRemove.Count > 0)
            {
                RemoveOverlaysInternal(toRemove);
            }
        }

        private bool ComputeIsPartial(Thing thing, DirectHaulStatus status, LocalTargetInfo target)
        {
            if (status == DirectHaulStatus.Pending && target.IsValid)
            {
                var targetCell = target.Cell;
                var multiplePending = _pendingCellsMultiple.Contains(targetCell);
                var targetHeld = _heldCells.Contains(targetCell);
                return multiplePending || targetHeld;
            }

            if (status == DirectHaulStatus.Held)
            {
                var heldCell = thing.PositionHeld;
                if (heldCell.IsValid)
                {
                    var hasPendingHere = _pendingCells.Contains(heldCell);
                    return hasPendingHere;
                }
            }

            return false;
        }

        private void UpsertOverlay(
            Thing thing,
            DirectHaulStatus status,
            LocalTargetInfo target,
            bool isPartial
        )
        {
            if (_entries.TryGetValue(thing, out var entry))
            {
                var statusChanged = entry.Status != status;
                entry.Status = status;
                entry.Target = target;
                if (statusChanged)
                {
                    var texturePath = GetOverlayTexturePath(status, isPartial);
                    if (texturePath == null)
                    {
                        RemoveOverlaysInternal(new[] { thing });
                        return;
                    }
                    entry.IsPartial = isPartial;
                    entry.Overlay.UpdateTexturePath(texturePath);
                }
                else if (entry.IsPartial != isPartial)
                {
                    entry.IsPartial = isPartial;
                    var texturePath = GetOverlayTexturePath(status, isPartial);
                    if (texturePath != null)
                    {
                        entry.Overlay.UpdateTexturePath(texturePath);
                    }
                }
            }
            else
            {
                var overlay = new GraphicObject_StatusOverlay(thing);
                if (
                    _graphicsManager.RegisterGraphicObject(overlay)
                    is not GraphicObject_StatusOverlay registered
                )
                {
                    return;
                }
                overlay = registered;

                var texturePath = GetOverlayTexturePath(status, isPartial);
                if (texturePath == null)
                {
                    _graphicsManager.UnregisterGraphicObject(overlay.Key);
                    return;
                }
                overlay.UpdateTexturePath(texturePath);
                ApplyFadeIn(overlay);

                _entries[thing] = new ManagedOverlay
                {
                    Overlay = overlay,
                    Status = status,
                    IsPartial = isPartial,
                    Target = target,
                };
            }
        }

        private void RemoveOverlaysInternal(IEnumerable<Thing> things)
        {
            foreach (var thing in things)
            {
                if (!_entries.TryGetValue(thing, out var entry))
                {
                    continue;
                }

                if (entry.Overlay != null)
                {
                    ApplyFadeOut(entry.Overlay);
                    _graphicsManager.UnregisterGraphicObject(entry.Overlay.Key);
                }

                _currentlyMinAlpha.Remove(thing);
                _entries.Remove(thing);
            }
        }

        private void ClearInternal(bool applyFadeOut)
        {
            if (_entries.Count == 0)
            {
                return;
            }

            foreach (var kv in _entries)
            {
                var entry = kv.Value;
                if (applyFadeOut && entry.Overlay != null)
                {
                    ApplyFadeOut(entry.Overlay);
                }
                if (entry.Overlay != null)
                {
                    _graphicsManager.UnregisterGraphicObject(entry.Overlay.Key);
                }
            }

            _entries.Clear();
            _currentlyMinAlpha.Clear();
        }

        private void UpdateOverlayAlphasByProximity()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            var map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            var storageLens = map.GetPressRMapComponent()?.StorageLens;
            bool storageActive = storageLens is { IsActive: true };
            var mouseCell = Verse.UI.MouseCell();
            var mouseWorldPos = Verse.UI.MouseMapPosition();
            if (
                mouseCell == _lastMouseCell
                && storageActive == _lastStorageLensActive
                && (mouseWorldPos - _lastMouseWorldPos).sqrMagnitude < 1e-8f
            )
            {
                return;
            }
            _lastStorageLensActive = storageActive;
            _lastMouseCell = mouseCell;
            _lastMouseWorldPos = mouseWorldPos;

            var hoverSqr = HoverDistance * HoverDistance;

            var newMinAlphaThings = CollectHoveredThingsByDistance(mouseWorldPos, hoverSqr);
            if (storageActive)
            {
                MergeStorageLensThings(storageLens, newMinAlphaThings);
            }

            ApplyAlphaTweensInternal(newMinAlphaThings);
        }

        private HashSet<Thing> CollectHoveredThingsByDistance(Vector3 mouseWorldPos, float hoverSqr)
        {
            _hoveredThings.Clear();
            foreach (var (thing, entry) in _entries)
            {
                if (entry.Overlay == null)
                {
                    continue;
                }
                var dx = entry.Overlay.Position.x - mouseWorldPos.x;
                var dz = entry.Overlay.Position.z - mouseWorldPos.z;
                if (dx * dx + dz * dz <= hoverSqr)
                {
                    _hoveredThings.Add(thing);
                }
            }
            return _hoveredThings;
        }

        private void MergeStorageLensThings(
            PressR.Features.StorageLens.StorageLens storageLens,
            HashSet<Thing> things
        )
        {
            foreach (var thing in storageLens.State.StorableForSelectedStorageInView)
            {
                if (_entries.ContainsKey(thing))
                {
                    things.Add(thing);
                }
            }
        }

        private void ApplyAlphaTweensInternal(HashSet<Thing> newMinAlphaThings)
        {
            foreach (var thing in newMinAlphaThings)
            {
                if (_currentlyMinAlpha.Contains(thing))
                {
                    continue;
                }
                if (_entries.TryGetValue(thing, out var entry) && entry.Overlay != null)
                {
                    ApplyAlphaTween(entry.Overlay, FadeOutDuration, MinAlpha);
                }
            }

            foreach (var thing in _currentlyMinAlpha)
            {
                if (newMinAlphaThings.Contains(thing))
                {
                    continue;
                }
                if (_entries.TryGetValue(thing, out var entry) && entry.Overlay != null)
                {
                    ApplyAlphaTween(entry.Overlay, FadeInDuration, 1.0f);
                }
            }

            _currentlyMinAlpha.Clear();
            foreach (var thing in newMinAlphaThings)
            {
                _currentlyMinAlpha.Add(thing);
            }
        }

        private bool HasAnyVisibleTrackedThing(Map map, CellRect viewRect)
        {
            foreach (var thing in _thingStateManager.AllPendingThings)
            {
                if (thing == null)
                {
                    continue;
                }
                IntVec3 posToCheck;
                if (IsThingCarriedByVisiblePawn(thing, map, out var carrierPawn))
                {
                    posToCheck = carrierPawn.Position;
                }
                else
                {
                    posToCheck = thing.Position;
                }
                if (posToCheck.IsValid && viewRect.Contains(posToCheck))
                {
                    return true;
                }
            }

            foreach (var thing in _thingStateManager.AllHeldThings)
            {
                if (thing == null)
                {
                    continue;
                }
                var heldCell = thing.PositionHeld;
                if (heldCell.IsValid && viewRect.Contains(heldCell))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsThingCarriedByVisiblePawn(Thing thing, Map map, out Pawn carrierPawn)
        {
            if (
                thing.ParentHolder is Pawn_CarryTracker carrier
                && carrier.pawn != null
                && !carrier.pawn.Destroyed
                && carrier.pawn.Map == map
                && carrier.pawn.Position.IsValid
            )
            {
                carrierPawn = carrier.pawn;
                return true;
            }
            carrierPawn = null;
            return false;
        }

        private static string GetOverlayTexturePath(DirectHaulStatus status, bool isPartial)
        {
            return status switch
            {
                DirectHaulStatus.Pending => isPartial ? TexPathPendingPart : TexPathPendingFull,
                DirectHaulStatus.Held => isPartial ? TexPathHeldPart : TexPathHeldFull,
                _ => null,
            };
        }

        private void ApplyFadeIn(GraphicObject_StatusOverlay overlay)
        {
            if (overlay == null)
            {
                return;
            }
            overlay.Alpha = 0f;
            ApplyAlphaTween(overlay, FadeInDuration, 1.0f);
        }

        private void ApplyFadeOut(GraphicObject_StatusOverlay overlay)
        {
            if (overlay == null)
            {
                return;
            }
            ApplyAlphaTween(overlay, FadeOutDuration, 0.0f);
        }

        private void ApplyAlphaTween(
            GraphicObject_StatusOverlay target,
            float duration,
            float targetAlpha
        )
        {
            if (target == null)
            {
                return;
            }

            var key = target.Key;
            _graphicsManager.ApplyTween<float>(
                key,
                getter: () => target.Alpha,
                setter: value =>
                {
                    target.Alpha = value;
                },
                endValue: targetAlpha,
                duration: duration,
                propertyId: AlphaPropertyId,
                easing: Equations.Linear,
                onComplete: null
            );
        }
    }
}
