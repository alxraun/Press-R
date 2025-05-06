using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PressR.Features.DirectHaul.Core;
using PressR.Features.TabLens.Graphics;
using PressR.Graphics;
using PressR.Graphics.Controllers;
using PressR.Graphics.GraphicObjects;
using PressR.Graphics.Tween;
using PressR.Settings;
using UnityEngine;
using Verse;
using static Verse.UI;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStatusOverlayGraphicsController : IGraphicsController
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly HashSet<object> _managedOverlayKeys = new HashSet<object>();

        private const string TexPathPendingFull = "DirectHaul/pending_overlay_full";
        private const string TexPathPendingPart = "DirectHaul/pending_overlay_part_0";
        private const string TexPathHeldFull = "DirectHaul/held_overlay_full";
        private const string TexPathHeldPart = "DirectHaul/held_overlay_part_0";

        private const float HoverDistance = 0.25f;
        private const float FadeOutDuration = 0.035f;
        private const float FadeInDuration = 0.035f;
        private const float MinAlpha = 0.25f;

        private const float HoverDistanceSquared = HoverDistance * HoverDistance;

        public DirectHaulStatusOverlayGraphicsController(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
        }

        public void ConstantUpdate(Map map, bool isEnabled)
        {
            if (!isEnabled)
            {
                ClearInternal(true);
                return;
            }

            if (
                !TryPrepareOverlayData(
                    map,
                    out var exposableData,
                    out var visibleThingsWithStatus,
                    out var targetCellPendingCount,
                    out var heldThingCells
                )
            )
            {
                ClearInternal(true);
                return;
            }

            if (!visibleThingsWithStatus.Any())
            {
                ClearInternal(true);
                return;
            }

            SynchronizeManagedOverlays(
                visibleThingsWithStatus,
                exposableData,
                targetCellPendingCount,
                heldThingCells
            );
            UpdateActiveOverlays(
                visibleThingsWithStatus,
                exposableData,
                targetCellPendingCount,
                heldThingCells
            );
            UpdateProximityEffects(map);
        }

        private bool TryPrepareOverlayData(
            Map map,
            out DirectHaulExposableData exposableData,
            out Dictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            out Dictionary<IntVec3, int> targetCellPendingCount,
            out HashSet<IntVec3> heldThingCells
        )
        {
            visibleThingsWithStatus = new Dictionary<Thing, DirectHaulStatus>();
            targetCellPendingCount = new Dictionary<IntVec3, int>();
            heldThingCells = new HashSet<IntVec3>();

            exposableData = map?.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
            if (exposableData == null)
                return false;

            PrepareOverlayDataInternal(
                exposableData,
                map,
                visibleThingsWithStatus,
                targetCellPendingCount,
                heldThingCells
            );
            return true;
        }

        private void PrepareOverlayDataInternal(
            DirectHaulExposableData exposableData,
            Map map,
            Dictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            if (map == null)
                return;

            var viewRect = Find.CameraDriver.CurrentViewRect;
            var fogGrid = map.fogGrid;

            var trackedThings = exposableData.GetAllTrackedThings();

            foreach (var thing in trackedThings)
            {
                if (thing == null || thing.Destroyed)
                    continue;

                var status = exposableData.GetStatusForThing(thing);
                if (status == DirectHaulStatus.None)
                    continue;

                IntVec3 positionToCheck;
                LocalTargetInfo targetInfo = LocalTargetInfo.Invalid;
                bool isVisible;

                if (status == DirectHaulStatus.Pending)
                {
                    if (
                        exposableData.TryGetInfoFromPending(thing, out targetInfo, out _)
                        && targetInfo.IsValid
                    )
                    {
                        IntVec3 cell = targetInfo.Cell;
                        targetCellPendingCount.TryGetValue(cell, out int count);
                        targetCellPendingCount[cell] = count + 1;
                    }

                    if (IsThingCarriedByVisiblePawn(thing, map, out var carrierPawn))
                    {
                        positionToCheck = carrierPawn.Position;
                    }
                    else
                    {
                        positionToCheck = thing.Position;
                    }
                }
                else
                {
                    positionToCheck = GetRelevantPosition(thing, status);
                    if (positionToCheck.IsValid)
                    {
                        heldThingCells.Add(positionToCheck);
                    }
                }

                isVisible = positionToCheck.IsValid && viewRect.Contains(positionToCheck);

                if (isVisible)
                {
                    visibleThingsWithStatus[thing] = status;
                }
            }
        }

        private void SynchronizeManagedOverlays(
            IReadOnlyDictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            DirectHaulExposableData exposableData,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            List<Thing> thingsToRemove = null;
            foreach (object managedKey in _managedOverlayKeys)
            {
                if (
                    managedKey is ValueTuple<Thing, Type> keyTuple
                    && keyTuple.Item1 is Thing thingInManagedKey
                )
                {
                    if (!visibleThingsWithStatus.ContainsKey(thingInManagedKey))
                    {
                        thingsToRemove ??= new List<Thing>();
                        thingsToRemove.Add(thingInManagedKey);
                    }
                }
            }

            if (thingsToRemove != null && thingsToRemove.Any())
            {
                HandleRemovingOverlays(thingsToRemove);
            }

            List<Thing> thingsToAdd = null;
            foreach (Thing thingInVisible in visibleThingsWithStatus.Keys)
            {
                object potentialOverlayKey = (
                    thingInVisible,
                    typeof(DirectHaulStatusOverlayGraphicObject)
                );
                if (!_managedOverlayKeys.Contains(potentialOverlayKey))
                {
                    thingsToAdd ??= new List<Thing>();
                    thingsToAdd.Add(thingInVisible);
                }
            }

            if (thingsToAdd != null && thingsToAdd.Any())
            {
                HandleAddingOverlays(
                    thingsToAdd,
                    visibleThingsWithStatus,
                    exposableData,
                    targetCellPendingCount,
                    heldThingCells
                );
            }
        }

        private void HandleRemovingOverlays(IEnumerable<Thing> thingsToRemove)
        {
            foreach (var thingToRemove in thingsToRemove)
            {
                object overlayKey = (thingToRemove, typeof(DirectHaulStatusOverlayGraphicObject));

                if (_managedOverlayKeys.Contains(overlayKey))
                {
                    if (
                        _graphicsManager.TryGetGraphicObject(overlayKey, out var overlayBase)
                        && overlayBase is DirectHaulStatusOverlayGraphicObject overlay
                    )
                    {
                        ApplyFadeOut(overlay);
                    }
                    _graphicsManager.UnregisterGraphicObject(overlayKey);
                    _managedOverlayKeys.Remove(overlayKey);
                }
            }
        }

        private void HandleAddingOverlays(
            IEnumerable<Thing> thingsToAdd,
            IReadOnlyDictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            DirectHaulExposableData exposableData,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            if (!PressRMod.Settings.directHaulSettings.enableStatusOverlays)
                return;

            foreach (var thingToAdd in thingsToAdd)
            {
                var newOverlay = new DirectHaulStatusOverlayGraphicObject(thingToAdd);
                object newOverlayKey = newOverlay.Key;

                if (_graphicsManager.RegisterGraphicObject(newOverlay) != null)
                {
                    _managedOverlayKeys.Add(newOverlayKey);
                    UpdateOverlayVisualState(
                        newOverlay,
                        visibleThingsWithStatus[thingToAdd],
                        exposableData,
                        targetCellPendingCount,
                        heldThingCells
                    );
                    ApplyFadeIn(newOverlay);
                }
            }
        }

        private void UpdateActiveOverlays(
            IReadOnlyDictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            DirectHaulExposableData exposableData,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            foreach (var currentOverlayKey in _managedOverlayKeys.ToList())
            {
                if (
                    currentOverlayKey is not ValueTuple<Thing, Type> keyTuple
                    || keyTuple.Item1 is not Thing currentThing
                )
                {
                    _managedOverlayKeys.Remove(currentOverlayKey);
                    continue;
                }

                if (
                    !_graphicsManager.TryGetGraphicObject(currentOverlayKey, out var overlayBase)
                    || overlayBase is not DirectHaulStatusOverlayGraphicObject overlay
                )
                {
                    _managedOverlayKeys.Remove(currentOverlayKey);
                    continue;
                }

                if (visibleThingsWithStatus.TryGetValue(currentThing, out var status))
                {
                    UpdateOverlayVisualState(
                        overlay,
                        status,
                        exposableData,
                        targetCellPendingCount,
                        heldThingCells
                    );
                }
                else
                {
                    HandleRemovingOverlays(new List<Thing> { currentThing });
                }
            }
        }

        private void UpdateOverlayVisualState(
            DirectHaulStatusOverlayGraphicObject overlay,
            DirectHaulStatus status,
            DirectHaulExposableData exposableData,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            if (overlay?.Key == null)
                return;

            if (
                overlay.Key is not ValueTuple<Thing, Type> keyTuple
                || keyTuple.Item1 is not Thing currentThing
            )
                return;

            bool isPartial = ShouldUsePartialTexture(
                currentThing,
                status,
                exposableData,
                targetCellPendingCount,
                heldThingCells
            );
            string texturePath = GetOverlayTexturePath(status, isPartial);

            if (string.IsNullOrEmpty(texturePath))
            {
                HandleRemovingOverlays(new List<Thing> { currentThing });
            }
            else
            {
                overlay.UpdateVisualState(texturePath);
            }
        }

        private void UpdateProximityEffects(Map map)
        {
            UpdateOverlayAlphasByProximity(map);
        }

        public void Update() { }

        public void Clear()
        {
            ClearInternal(true);
        }

        private void ClearInternal(bool applyFadeOut = false)
        {
            if (_managedOverlayKeys.Count == 0)
                return;

            var keysToClear = _managedOverlayKeys.ToList();
            foreach (var overlayKey in keysToClear)
            {
                if (
                    applyFadeOut
                    && _graphicsManager.TryGetGraphicObject(overlayKey, out var overlayBase)
                    && overlayBase is DirectHaulStatusOverlayGraphicObject overlay
                )
                {
                    ApplyFadeOut(overlay);
                }
                _graphicsManager.UnregisterGraphicObject(overlayKey);
            }

            _managedOverlayKeys.Clear();
        }

        private IntVec3 GetRelevantPosition(Thing thing, DirectHaulStatus status)
        {
            return status switch
            {
                DirectHaulStatus.Pending => thing.Position,
                DirectHaulStatus.Held => thing.PositionHeld,
                _ => IntVec3.Invalid,
            };
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

        private bool ShouldUsePartialTexture(
            Thing thing,
            DirectHaulStatus status,
            DirectHaulExposableData exposableData,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            if (status == DirectHaulStatus.Pending)
            {
                if (
                    exposableData.TryGetInfoFromPending(
                        thing,
                        out LocalTargetInfo targetInfo,
                        out _
                    ) && targetInfo.IsValid
                )
                {
                    IntVec3 targetCell = targetInfo.Cell;
                    bool isMultiPendingTarget =
                        targetCellPendingCount.TryGetValue(targetCell, out int count) && count > 1;
                    bool isTargetCellHeld = heldThingCells.Contains(targetCell);
                    return isMultiPendingTarget || isTargetCellHeld;
                }
            }
            else if (status == DirectHaulStatus.Held)
            {
                IntVec3 currentCell = thing.PositionHeld;

                return targetCellPendingCount.ContainsKey(currentCell)
                    && targetCellPendingCount[currentCell] > 0;
            }

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

        private void UpdateOverlayAlphasByProximity(Map map)
        {
            if (map == null || !_managedOverlayKeys.Any())
                return;

            Vector3 mousePosition = MouseMapPosition();

            foreach (var overlayKey in _managedOverlayKeys)
            {
                if (
                    overlayKey is not ValueTuple<Thing, Type> keyTuple
                    || keyTuple.Item1 is not Thing thingKey
                )
                    continue;

                if (
                    !(
                        _graphicsManager.TryGetGraphicObject(overlayKey, out var overlayBase)
                        && overlayBase is DirectHaulStatusOverlayGraphicObject overlay
                    )
                )
                    continue;

                bool isThingOverlayActive = IsAssociatedThingOverlayActive(thingKey);
                float targetAlpha;
                bool isCloseToMouse = IsOverlayCloseToMouse(overlay.Position, mousePosition);

                if (isThingOverlayActive)
                {
                    targetAlpha = MinAlpha;
                }
                else
                {
                    targetAlpha = isCloseToMouse ? MinAlpha : 1.0f;
                }

                bool shouldFadeOutToMin = targetAlpha < 1.0f;
                float duration = shouldFadeOutToMin ? FadeOutDuration : FadeInDuration;

                if (Mathf.Approximately(overlay.Alpha, targetAlpha))
                {
                    continue;
                }

                ApplyAlphaTween(overlay, duration, targetAlpha);
            }
        }

        private bool IsOverlayCloseToMouse(Vector3 overlayPosition, Vector3 mousePosition)
        {
            float dx = overlayPosition.x - mousePosition.x;
            float dz = overlayPosition.z - mousePosition.z;
            return (dx * dx + dz * dz) <= HoverDistanceSquared;
        }

        private bool IsAssociatedThingOverlayActive(Thing targetThing)
        {
            object thingOverlayKey = (targetThing, typeof(TabLensThingOverlayGraphicObject));
            return _graphicsManager.TryGetGraphicObject(thingOverlayKey, out var thingOverlayGo);
        }

        private void ApplyFadeIn(DirectHaulStatusOverlayGraphicObject overlay)
        {
            if (overlay == null)
                return;
            overlay.Alpha = 0f;
            ApplyAlphaTween(overlay, FadeInDuration, 1.0f);
        }

        private void ApplyFadeOut(DirectHaulStatusOverlayGraphicObject overlay)
        {
            if (overlay == null)
                return;
            ApplyAlphaTween(overlay, FadeOutDuration, 0.0f);
        }

        private void ApplyAlphaTween(
            DirectHaulStatusOverlayGraphicObject target,
            float duration,
            float targetAlpha
        )
        {
            if (target?.Key == null)
                return;

            object key = target.Key;

            Action onCompleteAction = () => { };

            Guid newTweenId = _graphicsManager.ApplyTween<float>(
                key,
                getter: () => target.Alpha,
                setter: value =>
                {
                    if (target is IHasAlpha ha)
                        ha.Alpha = value;
                },
                endValue: targetAlpha,
                duration: duration,
                easing: Equations.Linear,
                onComplete: onCompleteAction,
                propertyId: nameof(IHasAlpha.Alpha)
            );
        }
    }
}
