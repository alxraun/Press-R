using System.Reflection;
using PressR.Features.TabLens.StorageLens;
using RimWorld;
using UnityEngine;

namespace PressR.Features.TabLens.StorageLens.Commands
{
    public class SetStorageTabScrollPositionCommand(StorageLensState state, Vector2 scrollPosition)
        : ICommand
    {
        private readonly StorageLensState _state = state;
        private readonly Vector2 _scrollPosition = scrollPosition;

        public void Execute()
        {
            if (_state == null || !_state.HasStorageTabUIData || _state.Inspector == null)
            {
                return;
            }

            if (_state.Inspector.OpenTabType != typeof(ITab_Storage))
            {
                return;
            }

            FieldInfo scrollPositionField = _state
                .ThingFilterState.GetType()
                .GetField(
                    "scrollPosition",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

            if (scrollPositionField != null)
            {
                scrollPositionField.SetValue(_state.ThingFilterState, _scrollPosition);
            }
        }
    }
}
