using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PressR.Features.TabLens.Graphics;
using PressR.Features.TabLens.StorageLens.Commands;
using PressR.Features.TabLens.StorageLens.Core;
using PressR.Graphics;
using PressR.Graphics.Effects;
using PressR.Graphics.GraphicObjects;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public static class StorageLensHelper
    {
        public static void RemoveObsoleteOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToRemove
        )
        {
            keysToRemove.ToList().ForEach(key => graphicsManager.UnregisterGraphicObject(key));
        }

        public static void AddNewOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToAdd,
            TrackedThingsData trackedThingsData,
            float fadeInDuration
        )
        {
            keysToAdd
                .ToList()
                .ForEach(key =>
                {
                    if (key is ValueTuple<Thing, Type> { Item1: var thing })
                    {
                        bool allowed = trackedThingsData.GetAllowanceState(thing);
                        Color color = GraphicsUtils.GetColorForState(allowed);

                        var graphicObject = new TabLensThingOverlayGraphicObject(
                            thing,
                            ShaderManager.HSVColorizeCutoutShader
                        );
                        graphicObject.Alpha = 0f;

                        if (graphicsManager.RegisterGraphicObject(graphicObject))
                        {
                            if (graphicObject is IHasColor colorTarget)
                            {
                                colorTarget.Color = color;
                            }

                            var fadeIn = new FadeInEffect(fadeInDuration);

                            graphicsManager.ApplyEffect(new[] { key }, fadeIn);
                        }
                    }
                });
        }

        public static void UpdateExistingOverlays(
            IGraphicsManager graphicsManager,
            IEnumerable<object> keysToUpdate,
            IReadOnlyDictionary<object, IGraphicObject> registeredObjects,
            TrackedThingsData trackedThingsData
        )
        {
            keysToUpdate
                .ToList()
                .ForEach(key =>
                {
                    if (key is ValueTuple<Thing, Type> { Item1: var thing })
                    {
                        if (registeredObjects.TryGetValue(key, out IGraphicObject graphicObject))
                        {
                            bool allowed = trackedThingsData.GetAllowanceState(thing);
                            Color desiredColor = GraphicsUtils.GetColorForState(allowed);

                            if (graphicObject is IHasColor colorTarget)
                            {
                                colorTarget.Color = desiredColor;
                            }
                        }
                    }
                });
        }

        public static void ClearAllOverlays(IGraphicsManager graphicsManager, float fadeOutDuration)
        {
            if (graphicsManager == null)
                return;

            var activeObjects = graphicsManager.GetActiveGraphicObjects();

            var storageLensKeys = activeObjects
                .Keys.Where(key =>
                    key is ValueTuple<Thing, Type> tuple
                    && tuple.Item2 == typeof(TabLensThingOverlayGraphicObject)
                )
                .ToList();

            foreach (var key in storageLensKeys)
            {
                var fadeInEffects = graphicsManager
                    .GetEffectsForTarget(key)
                    .OfType<FadeInEffect>()
                    .ToList();
                foreach (var effect in fadeInEffects)
                {
                    graphicsManager.StopEffect(effect.Key);
                }

                var fadeOut = new FadeOutEffect(fadeOutDuration);
                graphicsManager.ApplyEffect(new[] { key }, fadeOut);

                graphicsManager.UnregisterGraphicObject(key);
            }
        }

        public static HashSet<Thing> FilterItemsByParentSettings(
            IEnumerable<Thing> things,
            IStoreSettingsParent storageParent
        )
        {
            var parentSettingsFilter = storageParent?.GetParentStoreSettings()?.filter;
            var filteredThings = new HashSet<Thing>();

            if (parentSettingsFilter == null)
            {
                return filteredThings;
            }

            foreach (var thing in things)
            {
                if (thing?.def != null && parentSettingsFilter.Allows(thing.def))
                {
                    filteredThings.Add(thing);
                }
            }
            return filteredThings;
        }

        public static Dictionary<Thing, bool> CalculateItemAllowanceStates(
            IEnumerable<Thing> things,
            StorageSettings currentSettings
        )
        {
            var allowanceStates = new Dictionary<Thing, bool>();
            ThingFilter currentFilter = currentSettings?.filter;

            if (currentFilter == null)
            {
                foreach (var thing in things)
                {
                    if (thing != null)
                        allowanceStates[thing] = false;
                }
                return allowanceStates;
            }

            foreach (var thing in things)
            {
                if (thing?.def != null)
                {
                    allowanceStates[thing] = currentFilter.Allows(thing.def);
                }
            }
            return allowanceStates;
        }

        public static (UIStateSnapshot snapshot, StorageTabUIData uiData) FetchUIData()
        {
            var selector = Find.Selector;
            if (selector is null)
                return (null, null);
            var inspector = MainButtonDefOf.Inspect.TabWindow as MainTabWindow_Inspect;
            if (inspector is null)
                return (null, null);
            object selectedObject = selector.SingleSelectedObject;
            Type openTabType = inspector.OpenTabType;

            string storageSearchText = null;
            Vector2 scrollPosition = Vector2.zero;
            StorageTabUIData uiData = null;

            UIStateSnapshot CreateSnapshot() =>
                new UIStateSnapshot(
                    selectedObject,
                    openTabType,
                    storageSearchText,
                    scrollPosition,
                    inspector,
                    selector
                );

            ITab_Storage storageTabInstance = inspector
                .CurTabs?.OfType<ITab_Storage>()
                .FirstOrDefault();
            if (storageTabInstance is null)
                return (CreateSnapshot(), null);

            object thingFilterState = ReflectionUtils.GetFieldValue<object>(
                storageTabInstance,
                "thingFilterState"
            );
            if (thingFilterState is null)
                return (CreateSnapshot(), null);

            object quickSearchWidget = ReflectionUtils.GetFieldValue<object>(
                thingFilterState,
                "quickSearch"
            );
            if (quickSearchWidget is null)
                return (CreateSnapshot(), null);

            object quickSearchFilter = ReflectionUtils.GetFieldValue<object>(
                quickSearchWidget,
                "filter"
            );
            if (quickSearchFilter is null)
                return (CreateSnapshot(), null);

            PropertyInfo quickSearchTextProperty = ReflectionUtils.GetPropertyInfo(
                quickSearchFilter,
                "Text"
            );
            if (quickSearchTextProperty is null)
                return (CreateSnapshot(), null);

            scrollPosition = ReflectionUtils.GetFieldValue<Vector2>(
                thingFilterState,
                "scrollPosition"
            );
            storageSearchText = ReflectionUtils.GetPropertyValue<string>(quickSearchFilter, "Text");

            uiData = new StorageTabUIData(
                quickSearchWidget,
                quickSearchFilter,
                quickSearchTextProperty,
                inspector,
                selector,
                thingFilterState
            );

            return (CreateSnapshot(), uiData);
        }

        public static (
            SetStorageQuickSearchFromThingCommand.SearchTargetType focusType,
            ToggleAllowanceCommand.AllowanceToggleType toggleType
        ) GetInteractionTypesFromModifiers()
        {
            SetStorageQuickSearchFromThingCommand.SearchTargetType focusType;
            ToggleAllowanceCommand.AllowanceToggleType toggleType;

            bool mod100x = PressRInput.IsModifierIncrement100xKeyPressed;
            bool mod10x = PressRInput.IsModifierIncrement10xKeyPressed;

            if (mod100x && mod10x)
            {
                focusType = SetStorageQuickSearchFromThingCommand.SearchTargetType.Clear;
                toggleType = ToggleAllowanceCommand.AllowanceToggleType.All;
            }
            else if (mod100x)
            {
                focusType = SetStorageQuickSearchFromThingCommand.SearchTargetType.ParentCategory;
                toggleType = ToggleAllowanceCommand.AllowanceToggleType.ParentCategory;
            }
            else if (mod10x)
            {
                focusType = SetStorageQuickSearchFromThingCommand.SearchTargetType.Category;
                toggleType = ToggleAllowanceCommand.AllowanceToggleType.Category;
            }
            else
            {
                focusType = SetStorageQuickSearchFromThingCommand.SearchTargetType.Item;
                toggleType = ToggleAllowanceCommand.AllowanceToggleType.Item;
            }

            return (focusType, toggleType);
        }
    }
}
