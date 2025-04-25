using System.Reflection;
using PressR.Features.TabLens.Lenses.StorageLens.Core;
using PressR.Interfaces;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.Lenses.StorageLens.Commands
{
    public class ClearStorageTabSearchTextCommand : ICommand
    {
        private readonly StorageTabUIData _uiData;

        public ClearStorageTabSearchTextCommand(StorageTabUIData uiData)
        {
            _uiData = uiData;
        }

        public void Execute()
        {
            if (_uiData == null || !_uiData.IsValid)
                return;

            _uiData.QuickSearchTextProperty.SetValue(_uiData.QuickSearchFilter, "");
        }
    }
}
