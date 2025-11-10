using System;
using System.Collections.Generic;
using Microtools.Graphics;
using Microtools.Graphics.Tween;
using UnityEngine;
using Verse;

namespace Microtools.Features.StorageLens
{
    public sealed class GraphicsController_ThingOverlay(
        IGraphicsManager graphicsManager,
        State state
    )
    {
        private readonly IGraphicsManager _graphicsManager =
            graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
        private readonly State _state = state ?? throw new ArgumentNullException(nameof(state));
        private const float FadeInDuration = 0.05f;
        private const float FadeOutDuration = 0.05f;
        private const float SubsequentFadeInDuration = 0.2f;
        private bool _isInitialActivation = true;

        private readonly Dictionary<Thing, GraphicObject_ThingOverlay> _entries = new(256);
        private readonly List<Thing> _thingsToAddList = [];
        private readonly List<Thing> _thingsToUpdateList = [];
        private readonly List<Thing> _thingsToRemoveList = [];

        private static readonly Color AllowedColor = new(70f / 255f, 203f / 255f, 24f / 255f);
        private static readonly Color DisallowedColor = new(224f / 255f, 60f / 255f, 49f / 255f);

        public void Update()
        {
            if (
                _state?.StorableForSelectedStorageInView == null
                || !MicrotoolsMod.Settings.storageLensSettings.enableStorageLensOverlays
            )
            {
                ClearInternal();
                return;
            }

            var desiredThings = _state.StorableForSelectedStorageInView;

            _thingsToRemoveList.Clear();
            foreach (var existing in _entries.Keys)
            {
                if (!desiredThings.Contains(existing))
                {
                    _thingsToRemoveList.Add(existing);
                }
            }

            _thingsToAddList.Clear();
            _thingsToUpdateList.Clear();
            foreach (var thing in desiredThings)
            {
                if (_entries.ContainsKey(thing))
                {
                    _thingsToUpdateList.Add(thing);
                }
                else
                {
                    _thingsToAddList.Add(thing);
                }
            }

            RemoveOverlays(_thingsToRemoveList);

            float currentFadeInDuration = _isInitialActivation
                ? FadeInDuration
                : SubsequentFadeInDuration;

            AddNewOverlays(_thingsToAddList, currentFadeInDuration);
            bool anyReactivated = UpdateExistingOverlays(
                _thingsToUpdateList,
                currentFadeInDuration
            );

            if (_isInitialActivation && (_thingsToAddList.Count != 0 || anyReactivated))
            {
                _isInitialActivation = false;
            }
        }

        public void Clear()
        {
            ClearInternal();
        }

        private void ClearInternal()
        {
            if (_graphicsManager == null)
                return;

            _isInitialActivation = true;

            if (_entries.Count == 0)
                return;

            foreach (var kvp in _entries)
            {
                var overlay = kvp.Value;
                if (overlay != null && overlay.State == GraphicObjectState.Active)
                {
                    ApplyFadeOut(_graphicsManager, overlay.Key, overlay);
                    _graphicsManager.UnregisterGraphicObject(overlay.Key);
                }
            }

            _entries.Clear();
            _thingsToAddList.Clear();
            _thingsToUpdateList.Clear();
            _thingsToRemoveList.Clear();
        }

        private void RemoveOverlays(IEnumerable<Thing> thingsToRemove)
        {
            foreach (var thing in thingsToRemove)
            {
                if (_entries.TryGetValue(thing, out var overlay))
                {
                    if (overlay is IHasAlpha alphaTarget)
                    {
                        ApplyFadeOut(_graphicsManager, overlay.Key, alphaTarget);
                    }
                    _graphicsManager.UnregisterGraphicObject(overlay.Key);
                    _entries.Remove(thing);
                }
            }
        }

        private void AddNewOverlays(IEnumerable<Thing> thingsToAdd, float fadeInDuration)
        {
            foreach (var thing in thingsToAdd)
            {
                if (thing == null)
                {
                    continue;
                }

                _state.AllowanceStatesForSelectedStorage.TryGetValue(thing.def, out var allowed);
                Color color = allowed ? AllowedColor : DisallowedColor;

                var overlay = new GraphicObject_ThingOverlay(thing) { Alpha = 0f };

                var registeredObject = _graphicsManager.RegisterGraphicObject(overlay);

                if (registeredObject is IHasColor colorTarget)
                {
                    colorTarget.Color = color;
                }

                if (registeredObject is IHasAlpha alphaTarget)
                {
                    ApplyFadeIn(_graphicsManager, overlay.Key, alphaTarget, fadeInDuration);
                }

                _entries[thing] = overlay;
            }
        }

        private bool UpdateExistingOverlays(IEnumerable<Thing> thingsToUpdate, float fadeInDuration)
        {
            bool anyReactivated = false;
            foreach (var thing in thingsToUpdate)
            {
                if (thing == null)
                {
                    continue;
                }

                if (!_entries.TryGetValue(thing, out var overlay) || overlay == null)
                {
                    continue;
                }

                _state.AllowanceStatesForSelectedStorage.TryGetValue(thing.def, out var allowed);
                var desiredColor = allowed ? AllowedColor : DisallowedColor;
                if (overlay is IHasColor colorTarget)
                {
                    colorTarget.Color = desiredColor;
                }

                if (overlay.State == GraphicObjectState.PendingRemoval)
                {
                    var reactivated = _graphicsManager.RegisterGraphicObject(overlay);
                    if (reactivated is IHasAlpha alphaTarget)
                    {
                        ApplyFadeIn(_graphicsManager, overlay.Key, alphaTarget, fadeInDuration);
                    }
                    anyReactivated = true;
                }
            }
            return anyReactivated;
        }

        private static void ApplyFadeIn(
            IGraphicsManager graphicsManager,
            object key,
            IHasAlpha alphaTarget,
            float duration
        )
        {
            if (alphaTarget == null)
                return;

            graphicsManager.ApplyTween<float>(
                key,
                getter: () => alphaTarget.Alpha,
                setter: value => alphaTarget.Alpha = value,
                endValue: 1.0f,
                duration: duration,
                propertyId: nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }

        private static void ApplyFadeOut(
            IGraphicsManager graphicsManager,
            object key,
            IHasAlpha alphaTarget
        )
        {
            if (alphaTarget == null)
                return;

            graphicsManager.ApplyTween<float>(
                key,
                getter: () => alphaTarget.Alpha,
                setter: value => alphaTarget.Alpha = value,
                endValue: 0.0f,
                duration: FadeOutDuration,
                propertyId: nameof(IHasAlpha.Alpha),
                easing: Equations.Linear
            );
        }
    }
}
