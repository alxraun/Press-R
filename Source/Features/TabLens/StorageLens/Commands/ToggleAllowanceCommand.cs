using PressR.Features.TabLens.StorageLens;
using RimWorld;
using Verse;
using Verse.Sound;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class ToggleAllowanceCommand : ICommand
    {
        public enum AllowanceToggleType
        {
            Item,
            Category,
            ParentCategory,
            All,
        }

        private readonly Thing _thing;
        private readonly StorageLensState _state;
        private readonly AllowanceToggleType _toggleType;

        public ToggleAllowanceCommand(
            Thing thing,
            StorageLensState state,
            AllowanceToggleType toggleType
        )
        {
            _thing = thing;
            _state = state;
            _toggleType = toggleType;
        }

        public void Execute()
        {
            if (_state == null || _state.CurrentStorageSettings == null || _thing == null)
                return;

            switch (_toggleType)
            {
                case AllowanceToggleType.Item:
                    ToggleItem();
                    break;
                case AllowanceToggleType.Category:
                    ToggleCategory();
                    break;
                case AllowanceToggleType.ParentCategory:
                    ToggleParentCategory();
                    break;
                case AllowanceToggleType.All:
                    ToggleAll();
                    break;
            }
        }

        private void ToggleItem()
        {
            if (_thing?.def == null)
                return;

            ThingFilter filter = _state.CurrentStorageSettings.filter;
            ThingDef def = _thing.def;

            bool isAllowedBasedOnState = _state.GetAllowanceState(_thing);
            bool newState = !isAllowedBasedOnState;

            filter.SetAllow(def, newState);
            _state.AllowanceStates[_thing] = newState;

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleCategory()
        {
            ThingCategoryDef category = _thing.def.FirstThingCategory;
            if (category == null)
            {
                ToggleItem();
                return;
            }

            ThingFilter filter = _state.CurrentStorageSettings.filter;
            bool isThingAllowedBasedOnState = _state.GetAllowanceState(_thing);
            bool newState = !isThingAllowedBasedOnState;

            filter.SetAllow(category, newState);

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleParentCategory()
        {
            ThingCategoryDef firstLevelCategory = _thing.def.FirstThingCategory;
            if (firstLevelCategory == null)
            {
                ToggleItem();
                return;
            }

            ThingCategoryDef categoryToToggle = firstLevelCategory.parent;
            if (categoryToToggle == null || categoryToToggle == ThingCategoryDefOf.Root)
            {
                categoryToToggle = firstLevelCategory;
            }

            ThingFilter filter = _state.CurrentStorageSettings.filter;
            bool isThingAllowedBasedOnState = _state.GetAllowanceState(_thing);
            bool newState = !isThingAllowedBasedOnState;

            filter.SetAllow(categoryToToggle, newState);

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleAll()
        {
            ThingFilter filter = _state.CurrentStorageSettings.filter;

            bool isAllowedBasedOnState = _state.GetAllowanceState(_thing);
            bool newState = !isAllowedBasedOnState;

            if (newState)
            {
                ThingFilter parentFilter = _state.SelectedStorage?.GetParentStoreSettings()?.filter;
                filter.SetAllowAll(parentFilter);
            }
            else
            {
                filter.SetDisallowAll();
            }

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void PlayToggleSound(bool turnedOn)
        {
            if (turnedOn)
            {
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
        }

        private void NotifySettingsChanged()
        {
            if (_state.SelectedStorage is IStoreSettingsParent parent)
            {
                parent.Notify_SettingsChanged();
            }
        }
    }
}
