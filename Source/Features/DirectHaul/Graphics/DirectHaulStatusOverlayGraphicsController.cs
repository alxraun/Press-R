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
        private const float FadeOutDuration = 0.05f;
        private const float FadeInDuration = 0.05f;
        private const float MinAlpha = 0.25f;

        private const float HoverDistanceSquared = HoverDistance * HoverDistance;

        private bool _isEnabledGlobally = false;

        public DirectHaulStatusOverlayGraphicsController(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
        }

        public void ConstantUpdate(Map map, bool isEnabled)
        {
            _isEnabledGlobally = isEnabled;

            if (!isEnabled)
            {
                Clear();
                return;
            }

            if (!TryGetExposableData(map, out var exposableData))
            {
                Clear();
                return;
            }

            var visibleThingsWithStatus = new Dictionary<Thing, DirectHaulStatus>();
            var targetCellPendingCount = new Dictionary<IntVec3, int>();
            var heldThingCells = new HashSet<IntVec3>();

            PrepareOverlayData(
                exposableData,
                map,
                visibleThingsWithStatus,
                targetCellPendingCount,
                heldThingCells
            );

            if (!visibleThingsWithStatus.Any())
            {
                Clear();
                return;
            }

            var requiredThings = visibleThingsWithStatus.Keys.ToHashSet();
            var managedThings = _managedOverlayKeys
                .Select(key => ((ITuple)key)[0] as Thing)
                .Where(t => t != null)
                .ToHashSet();

            var thingsToRemove = managedThings.Except(requiredThings).ToList();
            var thingsToAdd = requiredThings.Except(managedThings).ToList();

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

            foreach (var thingToAdd in thingsToAdd)
            {
                if (PressRMod.Settings.directHaulSettings.enableStatusOverlays)
                {
                    var newOverlay = new DirectHaulStatusOverlayGraphicObject(thingToAdd);
                    object newOverlayKey = newOverlay.Key;

                    if (_graphicsManager.RegisterGraphicObject(newOverlay) != null)
                    {
                        _managedOverlayKeys.Add(newOverlayKey);
                        string initialTexturePath = GetOverlayTexturePath(
                            visibleThingsWithStatus[thingToAdd],
                            ShouldUsePartialTexture(
                                thingToAdd,
                                visibleThingsWithStatus[thingToAdd],
                                exposableData,
                                targetCellPendingCount,
                                heldThingCells
                            )
                        );
                        if (!string.IsNullOrEmpty(initialTexturePath))
                        {
                            newOverlay.UpdateVisualState(initialTexturePath);
                        }
                        ApplyFadeIn(newOverlay);
                    }
                }
            }

            foreach (var currentOverlayKey in _managedOverlayKeys.ToList())
            {
                if (
                    currentOverlayKey is not ValueTuple<Thing, Type> keyTuple
                    || keyTuple.Item1 is not Thing currentThing
                )
                    continue;

                if (!visibleThingsWithStatus.TryGetValue(currentThing, out var status))
                {
                    if (_managedOverlayKeys.Contains(currentOverlayKey))
                    {
                        _graphicsManager.UnregisterGraphicObject(currentOverlayKey);
                        _managedOverlayKeys.Remove(currentOverlayKey);
                    }
                    continue;
                }

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
                    if (_managedOverlayKeys.Contains(currentOverlayKey))
                    {
                        if (
                            _graphicsManager.TryGetGraphicObject(
                                currentOverlayKey,
                                out var overlayToRemove
                            )
                        )
                        {
                            ApplyFadeOut(overlayToRemove as DirectHaulStatusOverlayGraphicObject);
                        }
                        _graphicsManager.UnregisterGraphicObject(currentOverlayKey);
                        _managedOverlayKeys.Remove(currentOverlayKey);
                    }
                }
                else if (
                    _graphicsManager.TryGetGraphicObject(currentOverlayKey, out var overlayBase)
                    && overlayBase is DirectHaulStatusOverlayGraphicObject overlay
                )
                {
                    overlay.UpdateVisualState(texturePath);
                }
            }

            UpdateOverlayAlphasByProximity(map);
        }

        public void Update() { }

        public void Clear()
        {
            ClearInternal();
        }

        private void ClearInternal()
        {
            if (_managedOverlayKeys.Count == 0)
                return;

            var keysToClear = _managedOverlayKeys.ToList();
            foreach (var overlayKey in keysToClear)
            {
                if (
                    _graphicsManager.TryGetGraphicObject(overlayKey, out var overlayBase)
                    && overlayBase is DirectHaulStatusOverlayGraphicObject overlay
                )
                {
                    ApplyFadeOut(overlay);
                }
                _graphicsManager.UnregisterGraphicObject(overlayKey);
            }

            _managedOverlayKeys.Clear();
        }

        private bool TryGetExposableData(Map map, out DirectHaulExposableData exposableData)
        {
            exposableData = map?.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
            return exposableData != null;
        }

        private void PrepareOverlayData(
            DirectHaulExposableData exposableData,
            Map map,
            Dictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells
        )
        {
            if (exposableData == null || map == null)
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
                        positionToCheck = GetRelevantPosition(thing, status);
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

        private bool IsThingPositionVisible(
            Thing thing,
            Map map,
            CellRect viewRect,
            FogGrid fogGrid,
            DirectHaulStatus status
        )
        {
            IntVec3 positionToCheck;
            if (
                status == DirectHaulStatus.Pending
                && IsThingCarriedByVisiblePawn(thing, map, out var carrierPawn)
            )
            {
                positionToCheck = carrierPawn.Position;
            }
            else
            {
                positionToCheck = GetRelevantPosition(thing, status);
            }

            if (!positionToCheck.IsValid)
                return false;

            return viewRect.Contains(positionToCheck) && !fogGrid.IsFogged(positionToCheck);
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
                        && overlay.State != GraphicObjectState.PendingRemoval
                    )
                )
                    continue;

                bool shouldFadeOut = ShouldFadeOut(thingKey, overlay, mousePosition);
                ApplyProximityFade(overlay, shouldFadeOut);
            }
        }

        private bool ShouldFadeOut(
            Thing thing,
            DirectHaulStatusOverlayGraphicObject overlay,
            Vector3 mousePosition
        )
        {
            bool isCloseToMouse = IsOverlayCloseToMouse(overlay.Position, mousePosition);
            bool isThingOverlayActive = IsAssociatedThingOverlayActive(thing);
            return isCloseToMouse || isThingOverlayActive;
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
            return _graphicsManager.TryGetGraphicObject(thingOverlayKey, out var thingOverlayGo)
                && thingOverlayGo.State == GraphicObjectState.Active;
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

        private void ApplyProximityFade(
            DirectHaulStatusOverlayGraphicObject overlay,
            bool shouldFadeOut
        )
        {
            if (overlay == null)
                return;
            float targetAlpha = shouldFadeOut ? MinAlpha : 1.0f;
            float duration = shouldFadeOut ? FadeOutDuration : FadeInDuration;

            if (Mathf.Approximately(overlay.Alpha, targetAlpha))
            {
                return;
            }

            ApplyAlphaTween(overlay, duration, targetAlpha);
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
