using System;
using PressR.Features.TabLens.StorageLens.Commands;
using PressR.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public class StorageLensInputHandler
    {
        private readonly StorageLensState _state;

        public StorageLensInputHandler(StorageLensState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void ProcessHoverEvent()
        {
            Thing currentHoveredThing = InputUtils.GetInteractableThingUnderMouse(
                _state.CurrentMap,
                thing => _state.CurrentThings != null && _state.CurrentThings.Contains(thing)
            );
            Thing previousHoveredThing = _state.HoveredThing;

            if (currentHoveredThing == null && previousHoveredThing != null)
            {
                if (
                    _state.HasStorageTabUIData
                    && _state.HasUISnapshotData
                    && _state.Inspector != null
                    && Find.WindowStack.CurrentWindowGetsInput
                )
                {
                    new ClearStorageTabSearchTextCommand(_state).Execute();
                    new SetStorageTabScrollPositionCommand(
                        _state,
                        _state.UISnapshot_StorageTabScrollPosition
                    ).Execute();
                }
                _state.LastHoverFocusType = null;
            }
            else if (currentHoveredThing != null)
            {
                var (currentFocusType, _) = GetInteractionTypesFromModifiers();

                bool needsCommandCall =
                    (currentHoveredThing != previousHoveredThing)
                    || (currentFocusType != _state.LastHoverFocusType);

                if (needsCommandCall && _state.HasStorageTabUIData && _state.HasStorageSettings)
                {
                    if (PressRMod.Settings.tabLensSettings.openStorageTabAutomatically)
                    {
                        new OpenStorageTabCommand(
                            _state.SelectedStorage,
                            _state.Inspector,
                            _state.Selector
                        ).Execute();
                    }

                    new SetStorageQuickSearchFromThingCommand(
                        currentHoveredThing,
                        _state,
                        currentFocusType
                    ).Execute();

                    _state.LastHoverFocusType = currentFocusType;
                }
            }

            _state.HoveredThing = currentHoveredThing;
        }

        public void ProcessClickEvent()
        {
            Thing clickedThing = InputUtils.GetInteractableThingUnderMouse(
                _state.CurrentMap,
                thing => _state.CurrentThings != null && _state.CurrentThings.Contains(thing)
            );

            if (clickedThing != null)
            {
                ProcessThingClick(clickedThing);
            }
        }

        private void ProcessThingClick(Thing clickedThing)
        {
            var (focusType, toggleType) = GetInteractionTypesFromModifiers();

            if (
                PressRMod.Settings.tabLensSettings.openStorageTabAutomatically
                && PressRMod.Settings.tabLensSettings.FocusItemInTabOnClick
                && _state.HasStorageSettings
                && _state.Inspector != null
                && _state.Selector != null
            )
            {
                new OpenStorageTabCommand(
                    _state.SelectedStorage,
                    _state.Inspector,
                    _state.Selector
                ).Execute();
            }

            if (
                PressRMod.Settings.tabLensSettings.FocusItemInTabOnClick
                && !PressRMod.Settings.tabLensSettings.FocusItemInTabOnHover
                && _state.HasStorageSettings
                && _state.HasStorageTabUIData
            )
            {
                new SetStorageQuickSearchFromThingCommand(
                    clickedThing,
                    _state,
                    focusType
                ).Execute();
            }

            if (_state.HasStorageSettings)
            {
                new ToggleAllowanceCommand(clickedThing, _state, toggleType).Execute();
            }
        }

        private (
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
