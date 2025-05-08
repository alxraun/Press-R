using PressR.Features.TabLens.StorageLens;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class SetSelectionCommand(object targetSelection, StorageLensState state) : ICommand
    {
        private readonly object _targetSelection = targetSelection;
        private readonly StorageLensState _state = state;

        public void Execute()
        {
            if (_state == null || _state.Selector == null)
                return;

            var selector = _state.Selector;

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
            if (_targetSelection is Thing thing && (thing.Destroyed || !thing.Spawned))
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
