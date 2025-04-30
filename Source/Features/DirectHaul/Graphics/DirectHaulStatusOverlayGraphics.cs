using System;
using System.Collections.Generic;
using System.Linq;
using PressR.Features.DirectHaul.Core;
using PressR.Features.DirectHaul.Graphics;
using PressR.Features.TabLens.Graphics;
using PressR.Graphics;
using PressR.Graphics.Effects;
using PressR.Graphics.GraphicObjects;
using PressR.Settings;
using UnityEngine;
using Verse;
using static Verse.UI;

namespace PressR.Features.DirectHaul.Graphics
{
    public class DirectHaulStatusOverlayGraphics
    {
        private readonly IGraphicsManager _graphicsManager;
        private readonly Dictionary<
            Thing,
            DirectHaulStatusOverlayGraphicObject
        > _activeStatusOverlays = new Dictionary<Thing, DirectHaulStatusOverlayGraphicObject>();
        private readonly Dictionary<object, Guid> _activeEffectIds = new Dictionary<object, Guid>();

        private const string TexPathPendingFull = "DirectHaul/pending_overlay_full";
        private const string TexPathPendingPart = "DirectHaul/pending_overlay_part_0";
        private const string TexPathHeldFull = "DirectHaul/held_overlay_full";
        private const string TexPathHeldPart = "DirectHaul/held_overlay_part_0";

        private const float HoverDistance = 0.25f;
        private const float FadeOutDuration = 0.05f;
        private const float FadeInDuration = 0.05f;
        private const float MinAlpha = 0.25f;

        private const float HoverDistanceSquared = HoverDistance * HoverDistance;

        public DirectHaulStatusOverlayGraphics(IGraphicsManager graphicsManager)
        {
            _graphicsManager =
                graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
        }

        public void UpdateDirectHaulStatusOverlays(Map map)
        {
            if (!TryGetExposableData(map, out var exposableData))
            {
                ClearAllOverlays();
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
                ClearAllOverlays();
                return;
            }

            RemoveObsoleteOverlays(visibleThingsWithStatus.Keys.ToHashSet());
            ProcessOverlayUpdates(
                visibleThingsWithStatus,
                targetCellPendingCount,
                heldThingCells,
                exposableData
            );
            UpdateOverlayAlphasByProximity(map);
        }

        public void ClearAllOverlays()
        {
            if (_activeStatusOverlays.Count == 0)
                return;

            var thingsToClear = _activeStatusOverlays.Keys.ToList();
            foreach (var thing in thingsToClear)
            {
                UnregisterAndRemoveOverlay(thing);
            }

            _activeStatusOverlays.Clear();
            _activeEffectIds.Clear();
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

        private void RemoveObsoleteOverlays(HashSet<Thing> currentVisibleThings)
        {
            var currentlyDisplayedThings = _activeStatusOverlays.Keys.ToHashSet();
            var thingsToRemove = currentlyDisplayedThings.Except(currentVisibleThings).ToList();

            foreach (var thingToRemove in thingsToRemove)
            {
                UnregisterAndRemoveOverlay(thingToRemove);
            }
        }

        private void ProcessOverlayUpdates(
            Dictionary<Thing, DirectHaulStatus> visibleThingsWithStatus,
            Dictionary<IntVec3, int> targetCellPendingCount,
            HashSet<IntVec3> heldThingCells,
            DirectHaulExposableData exposableData
        )
        {
            foreach (var (thing, status) in visibleThingsWithStatus)
            {
                if (thing == null || thing.Destroyed || !thing.SpawnedOrAnyParentSpawned)
                {
                    UnregisterAndRemoveOverlay(thing);
                    continue;
                }

                bool isPartial = ShouldUsePartialTexture(
                    thing,
                    status,
                    exposableData,
                    targetCellPendingCount,
                    heldThingCells
                );
                string texturePath = GetOverlayTexturePath(status, isPartial);

                if (string.IsNullOrEmpty(texturePath))
                {
                    UnregisterAndRemoveOverlay(thing);
                }
                else
                {
                    UpdateOrCreateOverlay(thing, texturePath);
                }
            }
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

        private void UpdateOrCreateOverlay(Thing thing, string texturePath)
        {
            if (_activeStatusOverlays.TryGetValue(thing, out var existingOverlay))
            {
                existingOverlay.UpdateVisualState(texturePath);
            }
            else
            {
                if (PressRMod.Settings.directHaulSettings.enableStatusOverlays)
                {
                    RegisterAndAddOverlay(thing, texturePath);
                }
            }
        }

        private void RegisterAndAddOverlay(Thing thing, string texturePath)
        {
            var newOverlay = new DirectHaulStatusOverlayGraphicObject(thing);
            if (_graphicsManager.RegisterGraphicObject(newOverlay))
            {
                newOverlay.UpdateVisualState(texturePath);
                _activeStatusOverlays.Add(thing, newOverlay);
            }
        }

        private void UnregisterAndRemoveOverlay(Thing thing)
        {
            if (_activeStatusOverlays.TryGetValue(thing, out var overlay))
            {
                StopAndRemoveEffect(overlay.Key);
                _graphicsManager.UnregisterGraphicObject(overlay.Key);
                _activeStatusOverlays.Remove(thing);
            }
        }

        private void UpdateOverlayAlphasByProximity(Map map)
        {
            if (map == null || !_activeStatusOverlays.Any())
                return;

            Vector3 mousePosition = MouseMapPosition();

            foreach (var (thing, overlay) in _activeStatusOverlays)
            {
                if (overlay == null || overlay.State != GraphicObjectState.Active)
                    continue;

                bool shouldFadeOut = ShouldFadeOut(thing, overlay, mousePosition);

                ManageFadeEffect(overlay, shouldFadeOut);
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

        private void ManageFadeEffect(
            DirectHaulStatusOverlayGraphicObject overlay,
            bool shouldFadeOut
        )
        {
            if (shouldFadeOut)
            {
                ApplyFadeEffect<FadeOutEffect>(overlay, FadeOutDuration, MinAlpha);
            }
            else
            {
                if (overlay.Alpha < 1.0f)
                {
                    ApplyFadeEffect<FadeInEffect>(overlay, FadeInDuration, 1.0f);
                }
                else
                {
                    StopAndRemoveEffect(overlay.Key);
                }
            }
        }

        private void ApplyFadeEffect<T>(IHasAlpha target, float duration, float targetAlpha)
            where T : IEffect
        {
            var effectKey = (target as IGraphicObject)?.Key;
            if (effectKey == null)
                return;

            if (_activeEffectIds.TryGetValue(effectKey, out var existingEffectId))
            {
                var activeEffects = _graphicsManager.GetEffectsForTarget(effectKey);
                bool correctEffectActive = activeEffects.Any(e =>
                    e is T && e.Key == existingEffectId
                );

                if (correctEffectActive)
                    return;

                _graphicsManager.StopEffect(existingEffectId);
                _activeEffectIds.Remove(effectKey);
            }

            IEffect newEffect = typeof(T).Name switch
            {
                nameof(FadeOutEffect) => new FadeOutEffect(duration, targetAlpha),
                nameof(FadeInEffect) => new FadeInEffect(duration, targetAlpha),
                _ => throw new ArgumentException($"Unsupported effect type: {typeof(T).Name}"),
            };

            var keys = new List<object> { effectKey };
            var newEffectId = _graphicsManager.ApplyEffect(keys, newEffect);
            if (newEffectId != Guid.Empty)
            {
                _activeEffectIds[effectKey] = newEffectId;
            }
        }

        private void StopAndRemoveEffect(object targetKey)
        {
            if (_activeEffectIds.TryGetValue(targetKey, out var effectIdToStop))
            {
                _graphicsManager.StopEffect(effectIdToStop);
                _activeEffectIds.Remove(targetKey);
            }
        }
    }
}
