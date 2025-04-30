using System;
using System.Linq;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class OpenStorageTabCommand : ICommand
    {
        private readonly IStoreSettingsParent _storageParent;
        private readonly MainTabWindow_Inspect _inspector;
        private readonly Selector _selector;

        public OpenStorageTabCommand(
            IStoreSettingsParent storageParent,
            MainTabWindow_Inspect inspector,
            Selector selector
        )
        {
            _storageParent = storageParent;
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public void Execute()
        {
            if (_storageParent == null || _inspector == null || _selector == null)
                if (_storageParent == null || _inspector == null || _selector == null)
                    return;

            var inspector = _inspector;
            var selector = _selector;

            if (
                selector.SingleSelectedObject != _storageParent
                && _storageParent is Thing storageParentThing
            )
            {
                selector.ClearSelection();
                selector.Select(storageParentThing);
            }

            new SetOpenTabCommand(typeof(ITab_Storage), _inspector, _selector).Execute();
        }
    }
}
