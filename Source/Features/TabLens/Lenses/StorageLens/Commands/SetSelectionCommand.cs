using PressR.Features.TabLens.Lenses.StorageLens.Core;
using PressR.Interfaces;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.Lenses.StorageLens.Commands
{
    public class SetSelectionCommand(object targetSelection, StorageTabUIData uiData) : ICommand
    {
        private readonly object _targetSelection = targetSelection;
        private readonly StorageTabUIData _uiData = uiData;

        public void Execute()
        {
            if (_uiData == null || _uiData.Selector == null)
                return;

            var selector = _uiData.Selector;

            object currentSelection = selector.SingleSelectedObject;

            if (_targetSelection == currentSelection)
            {
                return;
            }

            if (_targetSelection == null)
            {
                selector.ClearSelection();
                return;
            }

            bool canSelect = true;
            if (_targetSelection is Thing thing && !thing.Spawned)
            {
                canSelect = false;
            }

            if (canSelect)
            {
                selector.Select(_targetSelection);
            }
            else
            {
                selector.ClearSelection();
            }
        }
    }
}
