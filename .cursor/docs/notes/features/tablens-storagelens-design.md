# TabLens StorageLens Feature Design

This document outlines the design of the `StorageLens` feature within the `TabLens` system.

## Overview

The `StorageLens` provides an interactive overlay for items on the map when the player has an `IStoreSettingsParent` (like a shelf or zone) selected and the `ITab_Storage` tab open. It allows players to quickly view and modify the storage settings by interacting directly with the items displayed on the map.

```ASCII
[`TabLensFeature`] <>-- manages --<> [`StorageLens (ILens)`]
        |                                  |
        |                                  +-- uses --> [`StorageLensHelper`]
        |                                  |
        |                                  +-- uses --> [`Commands (ICommand)`]
        |                                  |
        |                                  +-- uses --> [`Data Structures (Core)`]
        |                                  |
        |                                  +-- uses --> [`IGraphicsManager`] --> [`TabLensThingOverlayGraphicObject`]
        V
    [`PressRMod`] (Manages Features)
```

## Components

### 1. `TabLensFeature` (`Source/Features/TabLens/TabLensFeature.cs`)

-   **Responsibility**: Manages the activation and lifecycle of different `ILens` implementations. It acts as the main entry point for the TabLens functionality.
-   **Interaction**:
    -   Activated/Deactivated by `PressRMain` based on global settings (`PressRMod.Settings.enableTabLens`).
    -   Holds a collection of registered `ILens` instances (currently only `StorageLens`).
    -   Attempts to activate a suitable lens in its `Update` cycle. If a lens activates (`TryActivate`), it becomes the `_activeLens`.
    -   Delegates `Update` calls to the `_activeLens`.
    -   Deactivates the `_activeLens` when necessary.

### 2. `StorageLens` (`Source/Features/TabLens/Lenses/StorageLens/StorageLens.cs`)

-   **Responsibility**: Implements the `ILens` interface for storage-related interactions. Contains the core logic for the feature: state management, input handling, overlay display, and command execution.
-   **Interaction**:
    -   **Activation (`TryActivate`)**: Checks if the feature is enabled (`PressRMod.Settings.tabLensSettings.enableStorageLens`), if an `IStoreSettingsParent` is selected, and if `ITab_Storage` is open. If conditions are met, it initializes its state (`TryInitializeLensState`), updates tracked items, and potentially refreshes overlays.
    -   **State Initialization (`TryInitializeLensState`)**: Fetches necessary UI data and storage settings using `StorageLensHelper.FetchUIData` and stores them in `_storageSettingsData`, `_storageTabUIData`, and `_UIStateSnapshot`.
    -   **Update (`Update`)**:
        -   Validates its current state (`IsValidStateForUpdate`). If invalid (e.g., storage deselected), it deactivates.
        -   Updates the list of tracked items and their allowance states (`UpdateTrackedThings`).
        -   Handles mouse hover (`HandleMouseHover`) based on settings (`FocusItemInTabOnHover`), potentially executing commands like `SetStorageQuickSearchFromThingCommand` and `OpenStorageTabCommand`.
        -   Refreshes visual overlays (`RefreshThingOverlays`) if enabled (`enableStorageLensOverlays`).
        -   Handles mouse clicks (`HandleMouseInput`, `ProcessThingClick`), executing `ToggleAllowanceCommand` and potentially `SetStorageQuickSearchFromThingCommand` / `OpenStorageTabCommand` based on settings.
    -   **Deactivation (`Deactivate`)**: Clears overlays using `StorageLensHelper.ClearAllOverlays`, optionally restores the previous UI state (`RestoreUIState`), and clears internal state (`ClearState`).
    -   **State Restoration (`RestoreUIState`)**: Uses commands (`SetSelectionCommand`, `SetOpenTabCommand`, `SetStorageQuickSearchCommand`, `SetStorageTabScrollPositionCommand`) to restore the UI to the state captured in `_UIStateSnapshot`.

### 3. `StorageLensHelper` (`Source/Features/TabLens/Lenses/StorageLens/StorageLensHelper.cs`)

-   **Responsibility**: Provides static utility methods used by `StorageLens` and associated commands. Encapsulates logic for interacting with game UI via reflection, managing overlays, filtering items, and interpreting input modifiers.
-   **Key Methods**:
    -   `FetchUIData`: Uses reflection to get references to `ITab_Storage` internal fields and properties (like search text, scroll position) and creates `UIStateSnapshot` and `StorageTabUIData`.
    -   `FilterItemsByParentSettings`: Filters a list of things based on the parent storage settings.
    -   `CalculateItemAllowanceStates`: Determines the allowance status (allowed/disallowed) for a list of things based on the *current* storage settings.
    -   Overlay Management (`RemoveObsoleteOverlays`, `AddNewOverlays`, `UpdateExistingOverlays`, `ClearAllOverlays`): Manages the lifecycle of `TabLensThingOverlayGraphicObject` instances via the `IGraphicsManager`.
    -   `GetInteractionTypesFromModifiers`: Determines the intended focus (`SearchTargetType`) and toggle (`AllowanceToggleType`) actions based on currently pressed modifier keys (`PressRInput`).

### 4. Commands (`Source/Features/TabLens/Lenses/StorageLens/Commands/`)

These classes implement the `ICommand` interface, encapsulating specific actions that modify the game state or UI. They are instantiated and executed primarily by `StorageLens`.

-   **`ClearStorageTabSearchTextCommand`**: Clears the text in the `ITab_Storage` search bar.
-   **`OpenStorageTabCommand`**: Ensures the `IStoreSettingsParent` is selected and opens the `ITab_Storage` for it. Uses `SetOpenTabCommand`.
-   **`SetOpenTabCommand`**: Opens a specific tab within the main inspector window by `Type`.
-   **`SetSelectionCommand`**: Changes the game's currently selected object (`Find.Selector.Select`).
-   **`SetStorageQuickSearchCommand`**: Sets the text in the `ITab_Storage` search bar.
-   **`SetStorageQuickSearchFromThingCommand`**: Determines the appropriate search text based on a `Thing` and a `SearchTargetType` (Item, Category, ParentCategory) and uses `SetStorageQuickSearchCommand` or `ClearStorageTabSearchTextCommand`.
-   **`SetStorageTabScrollPositionCommand`**: Sets the scroll position within the `ITab_Storage` view.
-   **`ToggleAllowanceCommand`**: Modifies the `StorageSettings.filter` based on the clicked `Thing` and `AllowanceToggleType` (Item, Category, ParentCategory, All). Notifies the owner of the settings change and plays a sound.

### 5. Data Structures (`Source/Features/TabLens/Lenses/StorageLens/Core/`)

These classes hold data relevant to the `StorageLens`'s operation.

-   **`StorageSettingsData`**: Simple container holding the currently selected `IStoreSettingsParent` and its associated `StorageSettings`.
-   **`StorageTabUIData`**: Caches reflected UI elements and properties from `ITab_Storage` (search widget, filter, text property, inspector, selector, state object) for efficient access by commands. Includes an `IsValid` check.
-   **`TrackedThingsData`**: Holds the state related to items currently being managed by the lens: `CurrentThings` (items visible and relevant), `AllowanceStates` (dictionary mapping Thing -> allowed status), and `HoveredThing`.
-   **`UIStateSnapshot`**: Stores the state of the UI (selected object, open tab, search text, scroll position, inspector/selector references) when the lens activates, allowing `RestoreUIState` to revert changes upon deactivation.

### 6. Graphics (`Source/Features/TabLens/Graphics/GraphicObjects/TabLensThingOverlayGraphicObject.cs`)

-   **Responsibility**: Implements `IGraphicObject` to render the colored overlay on top of items.
-   **Interaction**:
    -   Instantiated by `StorageLensHelper.AddNewOverlays`.
    -   Registered with the `IGraphicsManager`.
    -   Targets a specific `Thing`.
    -   Uses `ThingRenderDataReplicator` to get the mesh, matrix, and material of the target `Thing`.
    -   Creates a copy of the original material and applies a specified `OverlayShader` (defaulting to `ShaderManager.HSVColorizeCutoutShader`).
    -   Updates its color (`IHasColor`) and alpha (`IHasAlpha`) based on properties set by `StorageLensHelper` and `FadeInEffect`/`FadeOutEffect`.
    -   Configures a `MaterialPropertyBlock` using `ShaderManager.GetConfigurator` to pass color, alpha, and other parameters to the shader during rendering.
    -   Renders the mesh with the overlay material and property block slightly above the original thing's altitude.
    -   Checks for validity (`IsValid`) and sets its state to `PendingRemoval` if the target `Thing` is destroyed or despawned.
    -   Disposes of its created material when `Dispose` is called (typically by the `GraphicsManager` during unregistration).

## Interaction Flow (Simplified)

```ASCII
[`User Input (Hover/Click + Modifiers)`] -> [`StorageLens.Update`]
                                               |
         +-------------------------------------+-------------------------------------------+
         | (Input Handling)                    | (Overlay Refresh)                         | (State Update)
         V                                     V                                           V
[`StorageLensHelper.GetInteractionTypes`]  [`StorageLensHelper.UpdateExistingOverlays`]  [`StorageLensHelper.CalculateItemAllowanceStates`]
         |                                     [`StorageLensHelper.AddNewOverlays`]        [`TrackedThingsData` updated]
         |                                     [`StorageLensHelper.RemoveObsoleteOverlays`]
         V
[`Commands.Execute()`] --> [`Modify Game State/UI`]
 (e.g., ToggleAllowance,     (StorageSettings, ITab_Storage UI, Selector)
  SetStorageQuickSearch)
```
