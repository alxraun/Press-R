using PressR.Features.DirectHaul.Core;
using PressR.Utils;
using UnityEngine;
using Verse;

namespace PressR.Features.DirectHaul
{
    public class DirectHaulInput
    {
        public DirectHaulMode GetDirectHaulMode()
        {
            bool isStorageModifier = PressRInput.IsModifierIncrement10xKeyPressed;
            bool isHighPriorityModifier = PressRInput.IsModifierIncrement100xKeyPressed;

            bool useStorageModeLogic = PressRMod
                .Settings
                .directHaulSettings
                .invertStandardAndStorageKeys
                ? !isStorageModifier
                : isStorageModifier;

            if (isHighPriorityModifier)
            {
                return DirectHaulMode.HighPriority;
            }
            if (useStorageModeLogic)
            {
                return DirectHaulMode.Storage;
            }

            return DirectHaulMode.Standard;
        }

        public bool IsTriggerDown()
        {
            return PressRInput.IsMouseButtonDown;
        }

        public bool IsTriggerHeld()
        {
            return PressRInput.IsMouseButtonHeld;
        }

        public bool IsTriggerUp()
        {
            return PressRInput.IsMouseButtonUp;
        }

        public IntVec3 GetMouseCell()
        {
            return InputUtils.GetMouseMapCell();
        }

        public bool TryUseEvent()
        {
            if (Event.current != null)
            {
                Event.current.Use();
                return true;
            }
            return false;
        }
    }
}
