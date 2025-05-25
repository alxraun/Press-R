using PressR.Features.TabLens.StorageLens;
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
        private readonly StorageLensState _state;
        private readonly SearchTargetType _focusType;

        public SetStorageQuickSearchFromThingCommand(
            Thing thing,
            StorageLensState state,
            SearchTargetType focusType
        )
        {
            _thing = thing;
            _state = state;
            _focusType = focusType;
        }

        public void Execute()
        {
            if (_state == null)
                return;

            if (_focusType == SearchTargetType.Clear)
            {
                if (_state.HasStorageTabUIData)
                {
                    new ClearStorageTabSearchTextCommand(_state).Execute();
                }
                return;
            }

            string searchText = GetSearchText();
            if (searchText != null)
            {
                if (_state.HasStorageTabUIData)
                {
                    new SetStorageQuickSearchCommand(_state, searchText).Execute();
                }
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
