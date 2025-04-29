using System.Reflection;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class SetStorageQuickSearchCommand : ICommand
    {
        private readonly object _quickSearchFilter;
        private readonly PropertyInfo _quickSearchTextProperty;
        private readonly string _searchText;

        public SetStorageQuickSearchCommand(
            object quickSearchFilter,
            PropertyInfo quickSearchTextProperty,
            string searchText
        )
        {
            _quickSearchFilter = quickSearchFilter;
            _quickSearchTextProperty = quickSearchTextProperty;
            _searchText = searchText;
        }

        public void Execute()
        {
            if (_quickSearchTextProperty != null && _quickSearchFilter != null)
            {
                _quickSearchTextProperty.SetValue(_quickSearchFilter, _searchText);
            }
        }
    }
}
