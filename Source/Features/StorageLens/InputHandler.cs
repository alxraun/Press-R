using Verse;

namespace Microtools.Features.StorageLens
{
    public class InputHandler(State state, ActionDispatcher action, Input input)
    {
        private readonly State _state = state;
        private readonly ActionDispatcher _action = action;
        private readonly Input _input = input;
        private ActionDispatcher.SearchTargetType? _lastHoverFocusType;
        private Thing _hoveredThing;

        public void ProcessInputEvents()
        {
            if (MicrotoolsMod.Settings.storageLensSettings.FocusItemInTabOnHover)
            {
                ProcessHoverEvent();
            }
            else
            {
                _hoveredThing = null;
            }

            if (_input.IsMouseDown)
            {
                ProcessClickEvent();
            }
        }

        private void ProcessHoverEvent()
        {
            var currentHoveredThing = Utils.GetInteractableThingUnderMouse(
                _state.CurrentMap,
                thing => _state.StorableForSelectedStorageInView.Contains(thing)
            );
            var previousHoveredThing = _hoveredThing;

            if (currentHoveredThing == null && previousHoveredThing != null)
            {
                // Logic to clear search/scroll on hover end
                _action.ClearSearchText();
                _action.SetStorageTabScrollPosition(_state.UISnapshot_StorageTabScrollPosition);
                _lastHoverFocusType = null;
            }
            else if (currentHoveredThing != null)
            {
                var (currentFocusType, _) = GetInteractionTypesFromModifiers();

                var needsCommandCall =
                    (currentHoveredThing != previousHoveredThing)
                    || (currentFocusType != _lastHoverFocusType);

                if (needsCommandCall)
                {
                    if (MicrotoolsMod.Settings.storageLensSettings.openStorageTabAutomatically)
                    {
                        _action.OpenStorageTab();
                    }
                    _action.SetSearchTextFromThing(currentHoveredThing, currentFocusType);
                    _lastHoverFocusType = currentFocusType;
                }
            }

            _hoveredThing = currentHoveredThing;
        }

        private void ProcessClickEvent()
        {
            var clickedThing = Utils.GetInteractableThingUnderMouse(
                _state.CurrentMap,
                thing => _state.StorableForSelectedStorageInView.Contains(thing)
            );

            if (clickedThing != null)
            {
                var (focusType, toggleType) = GetInteractionTypesFromModifiers();

                if (MicrotoolsMod.Settings.storageLensSettings.openStorageTabAutomatically)
                {
                    _action.OpenStorageTab();
                }

                if (MicrotoolsMod.Settings.storageLensSettings.FocusItemInTabOnClick)
                {
                    _action.SetSearchTextFromThing(clickedThing, focusType);
                }

                _action.ToggleAllowance(clickedThing, toggleType);
            }
        }

        private (
            ActionDispatcher.SearchTargetType focusType,
            ActionDispatcher.AllowanceToggleType toggleType
        ) GetInteractionTypesFromModifiers()
        {
            ActionDispatcher.SearchTargetType focusType;
            ActionDispatcher.AllowanceToggleType toggleType;

            var mod100x = _input.IsModifierIncrement100xKeyDown;
            var mod10x = _input.IsModifierIncrement10xKeyDown;

            if (mod100x && mod10x)
            {
                focusType = ActionDispatcher.SearchTargetType.Clear;
                toggleType = ActionDispatcher.AllowanceToggleType.All;
            }
            else if (mod100x)
            {
                focusType = ActionDispatcher.SearchTargetType.ParentCategory;
                toggleType = ActionDispatcher.AllowanceToggleType.ParentCategory;
            }
            else if (mod10x)
            {
                focusType = ActionDispatcher.SearchTargetType.Category;
                toggleType = ActionDispatcher.AllowanceToggleType.Category;
            }
            else
            {
                focusType = ActionDispatcher.SearchTargetType.Item;
                toggleType = ActionDispatcher.AllowanceToggleType.Item;
            }

            return (focusType, toggleType);
        }
    }
}
