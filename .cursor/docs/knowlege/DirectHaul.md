## `DirectHaul` Feature

### Description

In the vanilla game, to move a `Thing` from location A to location B, you need to:
    1. Place an `IStoreSettingsParent` (`Zone_Stockpile` or any `Building_Storage`) at location B (or area).
    2. In the storage settings, check the `Allow` checkbox for the `Thing`.
    3. `Pawns` will then haul the items to the new storage.

`DirectHaul` provides a direct way to assign hauling tasks to `colonist`s. Simply select the desired `Thing`s in the `Selector`, activate `DirectHaul`, hover the mouse cursor over the destination, and press `LMB`.

### Controls

- `PressR_ModifierKey` + `LMB` - `Standard` mode: Hauls `Thing`s to the specified location and marks them as `held`.
- `PressR_ModifierKey` + `ModifierIncrement_10x` + `LMB` - `Storage` mode: Creates a stockpile at the specified location. If a stockpile already exists, the items will be marked as `Allowed` in its settings.
- `PressR_ModifierKey` + `ModifierIncrement_100x` + `LMB` - `HighPriority` mode: Same as `Standard` mode, but with the highest job priority.
