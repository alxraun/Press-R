using PressR.Features.TabLens.StorageLens.Core;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class SetStorageQuickSearchFromThingCommand : ICommand
    {
        public enum SearchTargetType
        {
            Item,
            Category,
            ParentCategory,
            Clear,
        }

        private readonly Thing _thing;
        private readonly IStoreSettingsParent _storageParent;
        private readonly StorageTabUIData _uiData;
        private readonly SearchTargetType _focusType;

        public SetStorageQuickSearchFromThingCommand(
            Thing thing,
            IStoreSettingsParent storageParent,
            StorageTabUIData uiData,
            SearchTargetType focusType
        )
        {
            _thing = thing;
            _storageParent = storageParent;
            _uiData = uiData;
            _focusType = focusType;
        }

        public void Execute()
        {
            if (_focusType == SearchTargetType.Clear)
            {
                new ClearStorageTabSearchTextCommand(_uiData).Execute();
                return;
            }

            string searchText = GetSearchText();
            if (searchText != null)
            {
                new SetStorageQuickSearchCommand(
                    _uiData.QuickSearchFilter,
                    _uiData.QuickSearchTextProperty,
                    searchText
                ).Execute();
            }
        }

        private string GetSearchText()
        {
            if (_thing?.def == null)
                return null;

            ThingCategoryDef firstCategory = _thing.def.FirstThingCategory;

            switch (_focusType)
            {
                case SearchTargetType.Item:
                    return _thing.def.LabelCap;
                case SearchTargetType.Category:
                    return firstCategory != null ? firstCategory.LabelCap : _thing.def.LabelCap;
                case SearchTargetType.ParentCategory:
                    if (
                        firstCategory?.parent != null
                        && firstCategory.parent != ThingCategoryDefOf.Root
                    )
                    {
                        return firstCategory.parent.LabelCap;
                    }
                    else
                    {
                        return firstCategory?.LabelCap ?? _thing.def.LabelCap;
                    }
                default:
                    return null;
            }
        }
    }
}
