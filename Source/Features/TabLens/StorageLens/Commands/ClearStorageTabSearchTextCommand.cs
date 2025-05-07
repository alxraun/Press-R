using System.Reflection;
using PressR.Features.TabLens.StorageLens;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class ClearStorageTabSearchTextCommand : ICommand
    {
        private readonly StorageLensState _state;

        public ClearStorageTabSearchTextCommand(StorageLensState state)
        {
            _state = state;
        }

        public void Execute()
        {
            if (_state == null || !_state.HasStorageTabUIData)
                return;

            _state.QuickSearchTextProperty.SetValue(_state.QuickSearchFilter, "");
        }
    }
}
