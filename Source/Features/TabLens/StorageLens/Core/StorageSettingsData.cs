using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Core
{
    public class StorageSettingsData
    {
        public IStoreSettingsParent SelectedStorage { get; }
        public StorageSettings CurrentStorageSettings { get; }

        public StorageSettingsData(
            IStoreSettingsParent selectedStorage,
            StorageSettings currentStorageSettings
        )
        {
            SelectedStorage = selectedStorage;
            CurrentStorageSettings = currentStorageSettings;
        }

        public bool IsValid => SelectedStorage != null && CurrentStorageSettings != null;
    }
}
