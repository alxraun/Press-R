using PressR.Features.TabLens.StorageLens.Core;
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
        private readonly StorageSettings _settings;
        private readonly TrackedThingsData _trackedThingsData;
        private readonly AllowanceToggleType _toggleType;

        public ToggleAllowanceCommand(
            Thing thing,
            StorageSettings settings,
            TrackedThingsData trackedThingsData,
            AllowanceToggleType toggleType
        )
        {
            _thing = thing;
            _settings = settings;
            _trackedThingsData = trackedThingsData;
            _toggleType = toggleType;
        }

        public void Execute()
        {
            if (_settings == null || _thing == null || _trackedThingsData == null)
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

            ThingFilter filter = _settings.filter;
            ThingDef def = _thing.def;

            bool isAllowedBasedOnDTO = _trackedThingsData.GetAllowanceState(_thing);
            bool newState = !isAllowedBasedOnDTO;

            filter.SetAllow(def, newState);

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

            ThingFilter filter = _settings.filter;
            bool isThingAllowedBasedOnDTO = _trackedThingsData.GetAllowanceState(_thing);
            bool newState = !isThingAllowedBasedOnDTO;

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

            ThingFilter filter = _settings.filter;
            bool isThingAllowedBasedOnDTO = _trackedThingsData.GetAllowanceState(_thing);
            bool newState = !isThingAllowedBasedOnDTO;

            filter.SetAllow(categoryToToggle, newState);

            NotifySettingsChanged();
            PlayToggleSound(newState);
        }

        private void ToggleAll()
        {
            ThingFilter filter = _settings.filter;

            bool isAllowedBasedOnDTO = _trackedThingsData.GetAllowanceState(_thing);
            bool newState = !isAllowedBasedOnDTO;

            if (newState)
            {
                ThingFilter parentFilter = _settings.owner?.GetParentStoreSettings()?.filter;
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
            if (_settings.owner is IStoreSettingsParent parent)
            {
                parent.Notify_SettingsChanged();
            }
        }
    }
}
