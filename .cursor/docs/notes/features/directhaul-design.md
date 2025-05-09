## 1. Introduction

`DirectHaul` allows the player to specify specific items and a target location for their movement, ensuring that moved items are not automatically picked up by standard hauling mechanisms (`Held` status) but remain available for use, mimicking the behavior of items in a stockpile.

## 2. User Interaction & Initiation

Handles player interaction, feature activation/deactivation, and initiation of the `DirectHaul` process in different modes.

### 2.1 Activation & Deactivation
- **Activation Conditions** (`DirectHaulFeature.TryActivate`):
  - `PressR_ModifierKey` is held.
  - `Selector.SelectedObjectsListForReading` contains at least one haulable `Thing` (`TryGetValidHaulableSelectionContext`).
- **Deactivation Conditions** (`DirectHaulFeature.Deactivate`):
  - `PressR_ModifierKey` is released (checked in `PressRMain.MainUpdateLoop`).
  - No more selected haulable items in `Selector`.
  - Current map is unavailable.
- **On Deactivation**: Clears all visual effects via `ClearAllVisuals()`.

### 2.2 Input Handling (Mouse & Keyboard)
- **Input Processing** (`DirectHaulFeature.Update`, `DirectHaulInput`):
  - Selection of haulable `Thing`s via standard `Selector`.
  - Holding a special modifier key.
  - Checking mouse button press/hold: `IsTriggerDown()`, `IsTriggerHeld()`, `IsTriggerUp()`.
  - Determining `DirectHaulMode` based on held modifiers.
  - Getting mouse coordinates to determine the target cell.

### 2.3 Drag State Management (`DirectHaulDragState`)
- **Drag States**:
  - `Idle`: Initial state.
  - `Dragging`: Active dragging (starts after exceeding `MinDragDistanceThreshold`).
  - `Completed`: Dragging finished.
- **Control Methods**:
  - `StartDrag`: Begin dragging.
  - `UpdateDrag`: Update state.
  - `EndDrag`: End dragging.
  - `Reset`: Reset state.
- **Distance Measurement**: `CalculateDragDistance` to determine drag length.

### 2.4 Target Cell Placement Logic (`DirectHaulPlacement`)
- **Cell Finding Strategies**:
  - `FindPlacementCells`: Finds cells for placing items.
  - `CalculatePlacementCellsBfs`: Breadth-first search (BFS) from a central cell.
  - `CalculatePlacementCellsInterpolated`: Interpolation between two cells (for drag mode).
- **Validation Checks**:
  - Within map bounds, passable, not blocked.
  - Not a target cell for another *pending* item.
  - Does not contain blocking `Thing`s.
- **Caching**: Caches the last valid cells for optimization.

### 2.5 Haul Modes (`DirectHaulMode`, `DirectHaulFeature`)
Determines the operating mode and performs the corresponding action:
- **Standard (`DirectHaulMode.Standard`)**:
  - Finds valid placement cells.
  - Marks selected items as `Pending` with specified target cells.
- **Storage (`DirectHaulMode.Storage`)**:
  - Finds/creates storage (`DirectHaulStorage`).
  - Configures storage to accept selected items.
  - Removes items from the DirectHaul tracking system.
- **HighPriority (`DirectHaulMode.HighPriority`)**:
  - Similar to Standard, but with high execution priority.

### 2.6 Thing State Management (`DirectHaulThingState`)
- **Item State Control**:
  - `MarkThingsAsPending`: Marks items as awaiting hauling.
  - `RemoveNonPendingSelectedThingsFromTracking`: Removes unselected items from tracking.
- **Helper Methods**:
  - `TryMarkSingleThingAsPending`: Marks a single item as pending.
  - `TryRemoveSingleThingFromTracking`: Removes an item from tracking.

### 2.7 Preview System (`DirectHaulPreview`)
- **Preview Functionality**:
  - `TryGetPreviewPositions`: Checks placement possibility and gets preview positions.
  - Checks context and cell availability.
  - Maps items to cells for visualization.

## 3. Visual Feedback

### 3.1 Ghost Graphics (`DirectHaulGhostGraphics`)
- **Preview Ghosts**:
  - Displays transparent copies of items in target cells.
  - Manages fade-in/fade-out effects.
  - Filters items within the field of view.
- **Pending Ghosts**:
  - Similar to Preview, but for items with Pending status.
  - Uses different colors and effects.

### 3.2 Status Overlays (`DirectHaulStatusOverlayGraphics`)
- **Overlay Management**:
  - Displayed for all items with `Pending` or `Held` status.
  - Managed by `DirectHaulStatusOverlayGraphics`.
  - Different textures for different states: full/partial.
- **Dynamic Effects**:
  - Changes transparency on mouse hover.
  - Intelligent updates when visible items change.

### 3.3 Radius Indicator (`DirectHaulRadiusIndicatorGraphics`)
- **Radius Indicator**:
  - Visualizes the action area when selecting items.
  - Smoothly changes size using effects.
  - Different colors for different modes.

### 3.4 Storage Highlighting (`DirectHaulStorageHighlightGraphics`, `DirectHaulStorageRectGraphics`)
- **Storage Highlighting**:
  - Highlights storage suitable for placement.
  - Dynamically changes color based on compatibility.
- **Selection Rectangle**:
  - Visualizes the area when creating/expanding storage.
  - Color indication corresponding to the storage type.

### 3.5 Sound Effects (`DirectHaulSoundPlayer`)
- **Sound Effects**:
  - Accompanies dragging with sound effects.
  - Updates sounds when the drag state changes.
  - Manages the completion of sound effects.

## 4. Core Data Management

### 4.1 `DirectHaulExposableData`
- **Type**: Class implementing `IExposable` for data persistence.
- **Statuses** (`DirectHaulStatus`):
  - `None`: Not tracked.
  - `Pending`: Awaiting hauling.
  - `Held`: Hauled and held.
- **Data Structure**:
  - `Dictionary<Thing, ThingState>` for tracking items.
  - `ThingState`: Stores status, target cell, priority flag.
- **Core Methods**:
  - `MarkThingAsPending`: Marks an item as pending.
  - `MarkThingAsHeld`: Changes status to "held".
  - `SetThingAsHeldAt`: Sets an item as held at a specific cell.
  - `RemoveThingFromTracking`: Removes from the tracking system.
  - `GetThingsWithStatus`: Gets items with a specific status.
  - `GetPendingThingsAndTargets`: Gets pending items and their targets.
  - `CleanupData`: Cleans up invalid entries.

### 4.2 `DirectHaulFrameData`
- **Cached Data Structure**:
  - Reference to `DirectHaulExposableData`.
  - Lists of items by category (selected, pending, held).
  - Placement cells and their caching.
- **Update Methods**:
  - `Update`: Updates all data.
  - `UpdatePendingStatusLocally`: Local status update.
  - `SetCalculatedPlacementCells`: Caches calculated cells.
- **Helper Methods**:
  - `ClearState`, `LoadExposedData`, `PopulateSelectedThings`, etc.

## 5. Storage Management (`DirectHaulStorage`)
- **Storage Management Functions**:
  - `FindStorageAt`: Finds storage in a cell.
  - `CreateStockpileZone`: Creates a new stockpile zone.
  - `GetOrCreateStockpileZone`: Gets an existing or creates a new zone.
  - `ToggleThingDefsAllowance`: Enables/disables permissions for item types.
  - `ExpandStockpileZone`: Expands an existing zone.

## 6. Job System Integration

### 6.1 WorkGiver (`WorkGiver_DirectHaul`)
- **Job Finding**:
  - `PotentialWorkThingsGlobal`: Finds items with `Pending` status.
  - `JobOnThing`: Creates a hauling job.
- **Checks**:
  - Availability of item and target cell.
  - Possibility of placement in the target cell.
  - Reservation of item and cell.

### 6.2 Job Driver (`JobDriver_DirectHaul`)
- **Job Execution**:
  - Reserves item and target cell.
  - Moves to the item and picks it up.
  - Hauls to the target cell and places it.
  - Updates the item's status in `DirectHaulExposableData`.
- **Handling Stack Splitting**:
  - Transfers status from the original item to the new one when splitting a stack.

## 7. Harmony Patches

### 7.1 `Patch_CompressibilityDecider_DetermineReferences`
- **Function**: Ensures tracked items are saved during map compression.
- **Mechanism**: Adds all tracked items to the `referencedThings` list.

### 7.2 `Patch_StoreUtility_TryFindBestBetterStoreCellFor`
- **Function**: Prevents automatic hauling of items with `Pending` or `Held` status.
- **Mechanism**: Interrupts the search for the best storage cell for tracked items.

### 7.3 `Patch_Thing_GetGizmos`
- **Function**: Adds gizmos for managing item status.
- **Gizmos**:
  - `CancelHeldStatusGizmo`: Cancels the held status of an item.
  - `CancelPendingStatusGizmo`: Cancels pending haul.

### 7.4 `Patch_Thing_TryAbsorbStack`
- **Function**: Prevents automatic stack merging with held items.
- **Exception**: Allows merging if a `DirectHaul` job is being performed with this item.

## 8. Workflow & State Transitions

```
[Select Items] -> [Activate DirectHaul] -> [Select Mode & Target Cell]
    |                                                    |
    v                                                    v
[Cancel via Gizmo] <----- [Pending] <--> [Hauling] --> [Held] 
    |                                                    |
    v                                                    v
[Standard Hauling System] <--------------------- [Cancel via Gizmo]
```

## 9. Integration Points
- `PressRMapComponent`: Stores and initializes `DirectHaulExposableData`.
- `PressRMain`: Manages feature activation/deactivation.
- `GraphicsManager`: Manages visual effects and overlays.
- `PressRDefOf`: Defines references to Defs (`JobDef`, etc.).
- `PressRMod.Settings`: Stores DirectHaul feature settings.
