## `TabLens` Feature

### Description

In the vanilla game, there are many `ITab`s where you can toggle checkboxes related to `Thing`s rendered on the map. For example:
- `ITab_Storage` - checkboxes mark `Item` `Thing`s that are allowed to be stored in a stockpile.
- `ITab_Pawn_Gear` - checkboxes mark `Apparel` and other `Thing`s that a `Pawn` will wear.

In vanilla, checkboxes in `ITab`s are toggled exclusively through the UI. However, `TabLens` allows interactive interaction with them via `Thing`s on the map, using `Lens`-specific logic and visualization through `TabLensOverlay`.

```ASCII
[`User selects object with ITab (e.g., Storage)`] --> [`TabLensFeature checks for compatible Lens`]
                                                          |
                                                          V
[`Compatible Lens (e.g., StorageLens) activates`] --> [`TabLensOverlay applied to relevant Things`]
```

### `Lens`

New `Lens`es can be added to `TabLens` for various `ITab`s. A `Lens` encapsulates the interaction logic and visualization for a specific `ITab`. Only one `Lens` can be active at a time.

Currently implemented:
- `StorageLens` for `ITab_Storage`

### `TabLensOverlay`

A `TabLensOverlay` can be applied to each `Thing` that the active `Lens` works with and is rendered on the map. This overlay visualizes the state managed by the `Lens` (e.g., the state of a checkbox in an `ITab`), usually through color changes.

`TabLensOverlay`:
- Receives rendering data (mesh, matrix, material) from `ThingRenderDataReplicator`.
- Renders using a custom shader (default `HSVColorizeCutoutShader`).
- Is positioned at `Altitudes.AltInc` height above the base position of the `Thing`.
- Dynamically updates its color and transparency based on state and effects (`IHasColor`, `IHasAlpha`).
- Is automatically removed when the `Thing` despawns or is destroyed.
