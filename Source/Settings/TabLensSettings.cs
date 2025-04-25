using UnityEngine;
using Verse;

namespace PressR.Settings
{
    public class TabLensSettings : IExposable
    {
        public const bool EnableStorageLensDefault = true;
        public const bool EnableStorageLensOverlaysDefault = true;
        public const bool RestoreUIStateOnDeactivateDefault = true;
        public const bool OpenStorageTabAutomaticallyDefault = false;
        public const bool FocusItemInTabOnClickDefault = true;
        public const bool FocusItemInTabOnHoverDefault = false;

        public bool enableStorageLens = EnableStorageLensDefault;
        public bool enableStorageLensOverlays = EnableStorageLensOverlaysDefault;
        public bool restoreUIStateOnDeactivate = RestoreUIStateOnDeactivateDefault;
        public bool openStorageTabAutomatically = OpenStorageTabAutomaticallyDefault;

        private bool _focusItemInTabOnClick = FocusItemInTabOnClickDefault;
        private bool _focusItemInTabOnHover = FocusItemInTabOnHoverDefault;

        public bool FocusItemInTabOnClick
        {
            get => _focusItemInTabOnClick;
            set
            {
                if (_focusItemInTabOnClick == value)
                    return;

                _focusItemInTabOnClick = value;
                if (!value)
                {
                    FocusItemInTabOnHover = false;
                }
            }
        }

        public bool FocusItemInTabOnHover
        {
            get => _focusItemInTabOnHover;
            set
            {
                bool newValue = value && _focusItemInTabOnClick;
                if (_focusItemInTabOnHover == newValue)
                    return;

                _focusItemInTabOnHover = newValue;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(
                ref enableStorageLens,
                "enableStorageLens",
                EnableStorageLensDefault
            );
            Scribe_Values.Look(
                ref enableStorageLensOverlays,
                "enableStorageLensOverlays",
                EnableStorageLensOverlaysDefault
            );
            Scribe_Values.Look(
                ref restoreUIStateOnDeactivate,
                "restoreUIStateOnDeactivate",
                RestoreUIStateOnDeactivateDefault
            );
            Scribe_Values.Look(
                ref openStorageTabAutomatically,
                "openStorageTabAutomatically",
                OpenStorageTabAutomaticallyDefault
            );
            Scribe_Values.Look(
                ref _focusItemInTabOnClick,
                "focusItemInTabOnClick",
                FocusItemInTabOnClickDefault
            );
            Scribe_Values.Look(
                ref _focusItemInTabOnHover,
                "focusItemInTabOnHover",
                FocusItemInTabOnHoverDefault
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                FocusItemInTabOnClick = _focusItemInTabOnClick;
                FocusItemInTabOnHover = _focusItemInTabOnHover;
            }
        }

        public void ResetToDefaults()
        {
            enableStorageLens = EnableStorageLensDefault;
            enableStorageLensOverlays = EnableStorageLensOverlaysDefault;
            restoreUIStateOnDeactivate = RestoreUIStateOnDeactivateDefault;
            openStorageTabAutomatically = OpenStorageTabAutomaticallyDefault;
            FocusItemInTabOnClick = FocusItemInTabOnClickDefault;
            FocusItemInTabOnHover = FocusItemInTabOnHoverDefault;
        }
    }
}
