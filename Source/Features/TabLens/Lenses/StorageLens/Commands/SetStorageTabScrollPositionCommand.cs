using System.Reflection;
using PressR.Features.TabLens.Lenses.StorageLens.Core;
using PressR.Interfaces;
using RimWorld;
using UnityEngine;

namespace PressR.Features.TabLens.Lenses.StorageLens.Commands
{
    public class SetStorageTabScrollPositionCommand(StorageTabUIData uiData, Vector2 scrollPosition)
        : ICommand
    {
        private readonly StorageTabUIData _uiData = uiData;
        private readonly Vector2 _scrollPosition = scrollPosition;

        public void Execute()
        {
            if (_uiData == null || !_uiData.IsValid || _uiData.ThingFilterState == null)
            {
                return;
            }

            if (_uiData.Inspector.OpenTabType != typeof(ITab_Storage))
            {
                return;
            }

            FieldInfo scrollPositionField = _uiData
                .ThingFilterState.GetType()
                .GetField(
                    "scrollPosition",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

            if (scrollPositionField != null)
            {
                scrollPositionField.SetValue(_uiData.ThingFilterState, _scrollPosition);
            }
        }
    }
}
