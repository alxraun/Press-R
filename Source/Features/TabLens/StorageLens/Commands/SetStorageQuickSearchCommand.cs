using System.Reflection;
using PressR.Features.TabLens.StorageLens;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class SetStorageQuickSearchCommand : ICommand
    {
        private readonly StorageLensState _state;
        private readonly string _searchText;

        public SetStorageQuickSearchCommand(StorageLensState state, string searchText)
        {
            _state = state;
            _searchText = searchText;
        }

        public void Execute()
        {
            if (
                _state != null
                && _state.QuickSearchTextProperty != null
                && _state.QuickSearchFilter != null
            )
            {
                _state.QuickSearchTextProperty.SetValue(_state.QuickSearchFilter, _searchText);
            }
        }
    }
}
