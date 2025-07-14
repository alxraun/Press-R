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

        private readonly Dictionary<Thing, DirectHaulStatus> _visibleThingsWithStatus = new();
        private readonly Dictionary<IntVec3, int> _targetCellPendingCount = new();
        private readonly HashSet<IntVec3> _heldThingCells = new();
        private readonly List<Thing> _thingsToRemove = new();
        private readonly List<Thing> _thingsToAdd = new();
        private readonly PressR.Utils.Throttler.Throttler _throttler = new(1);

        public DirectHaulStatusOverlayGraphicsController(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new System.ArgumentNullException(nameof(graphicsManager));
        }

        public void ConstantUpdate(Map map, bool isEnabled)
        {
            if (!isEnabled)
            {
                ClearInternal(true);
                return;
            }

            _visibleThingsWithStatus.Clear();
            _targetCellPendingCount.Clear();
            _heldThingCells.Clear();

            if (!TryPrepareOverlayData(map, out var exposableData))
            {
                ClearInternal(true);
                return;
            }

            if (!_visibleThingsWithStatus.Any())
            {
                ClearInternal(true);
                return;
            }

            SynchronizeManagedOverlays(exposableData);
            UpdateActiveOverlays(exposableData);
            if (_throttler.ShouldExecute())
            {
                UpdateProximityEffects(map);
            }
        }

        private bool TryPrepareOverlayData(Map map, out DirectHaulExposableData exposableData)
        {
            exposableData = map?.GetComponent<PressRMapComponent>()?.DirectHaulExposableData;
            if (exposableData == null)
                return false;

            PrepareOverlayDataInternal(exposableData, map);
            return true;
        }

        private void PrepareOverlayDataInternal(DirectHaulExposableData exposableData, Map map)
        {
            if (map == null)
                return;

            var viewRect = Find.CameraDriver.CurrentViewRect;

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
                        _targetCellPendingCount.TryGetValue(cell, out int count);
                        _targetCellPendingCount[cell] = count + 1;
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
                        _heldThingCells.Add(positionToCheck);
                    }
                }

                isVisible = positionToCheck.IsValid && viewRect.Contains(positionToCheck);

                if (isVisible)
                {
                    _visibleThingsWithStatus[thing] = status;
                }
            }
        }

        private void SynchronizeManagedOverlays(DirectHaulExposableData exposableData)
        {
            _thingsToRemove.Clear();
            foreach (object managedKey in _managedOverlayKeys)
            {
                if (
                    managedKey is ValueTuple<Thing, Type> keyTuple
                    && keyTuple.Item1 is Thing thingInManagedKey
                )
                {
                    if (!_visibleThingsWithStatus.ContainsKey(thingInManagedKey))
                    {
                        _thingsToRemove.Add(thingInManagedKey);
                    }
                }
            }

            if (_thingsToRemove.Any())
            {
                HandleRemovingOverlays(_thingsToRemove);
            }

            _thingsToAdd.Clear();
            foreach (Thing thingInVisible in _visibleThingsWithStatus.Keys)
            {
                object potentialOverlayKey = (
                    thingInVisible,
                    typeof(DirectHaulStatusOverlayGraphicObject)
                );
                if (!_managedOverlayKeys.Contains(potentialOverlayKey))
                {
                    _thingsToAdd.Add(thingInVisible);
                }
            }

            if (_thingsToAdd.Any())
            {
                HandleAddingOverlays(_thingsToAdd, exposableData);
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
            DirectHaulExposableData exposableData
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
                        _visibleThingsWithStatus[thingToAdd],
                        exposableData
                    );
                    ApplyFadeIn(newOverlay);
                }
            }
        }

        private void UpdateActiveOverlays(DirectHaulExposableData exposableData)
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

                if (_visibleThingsWithStatus.TryGetValue(currentThing, out var status))
                {
                    UpdateOverlayVisualState(overlay, status, exposableData);
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
            DirectHaulExposableData exposableData
        )
        {
            if (overlay?.Key == null)
                return;

            if (
                overlay.Key is not ValueTuple<Thing, Type> keyTuple
                || keyTuple.Item1 is not Thing currentThing
            )
                return;

            bool isPartial = ShouldUsePartialTexture(currentThing, status, exposableData);
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
            DirectHaulExposableData exposableData
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
                        _targetCellPendingCount.TryGetValue(targetCell, out int count) && count > 1;
                    bool isTargetCellHeld = _heldThingCells.Contains(targetCell);
                    return isMultiPendingTarget || isTargetCellHeld;
                }
            }
            else if (status == DirectHaulStatus.Held)
            {
                IntVec3 currentCell = thing.PositionHeld;

                return _targetCellPendingCount.ContainsKey(currentCell)
                    && _targetCellPendingCount[currentCell] > 0;
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
