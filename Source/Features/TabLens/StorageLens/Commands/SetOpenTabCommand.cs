using System;
using System.Linq;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class SetOpenTabCommand(
        Type targetTabType,
        MainTabWindow_Inspect inspector,
        Selector selector
    ) : ICommand
    {
        private readonly Type _targetTabType = targetTabType;
        private readonly MainTabWindow_Inspect _inspector =
            inspector ?? throw new ArgumentNullException(nameof(inspector));
        private readonly Selector _selector =
            selector ?? throw new ArgumentNullException(nameof(selector));

        public void Execute()
        {
            if (_inspector == null || _selector == null)
            {
                return;
            }

            Type currentTabType = _inspector.OpenTabType;

            if (_targetTabType == null)
            {
                if (currentTabType != null)
                {
                    _inspector.CloseOpenTab();
                }
                return;
            }

            object currentSelection = _selector.SingleSelectedObject;

            if (currentSelection == null)
            {
                if (currentTabType != null)
                {
                    _inspector.CloseOpenTab();
                }
                return;
            }

            InspectTabBase targetTabInstance = _inspector.CurTabs?.FirstOrDefault(t =>
                t.GetType() == _targetTabType
            );

            if (targetTabInstance != null)
            {
                if (currentTabType != _targetTabType)
                {
                    InspectPaneUtility.OpenTab(_targetTabType);
                }
            }
            else
            {
                if (currentTabType != null)
                {
                    _inspector.CloseOpenTab();
                }
            }
        }
    }
}
