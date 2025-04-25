## `StorageLens`

### Description

`StorageLens` is an implementation of `ILens` for `ITab_Storage`. When `StorageLens` is active and the `ITab_Storage` tab is open for the selected storage (`IStoreSettingsParent`), `StorageLens` displays a `TabLensOverlay` for all visible `Item`s on the map that are allowed by the parent storage settings. The overlay color indicates whether the `Item` is allowed in the current storage settings (`StorageSettings`).

`StorageLens` allows interactive modification of `ITab_Storage` settings by clicking on `Item`s on the map and optionally interacts with the tab's UI on hover.

```ASCII
[`StorageLens Active`] --> [`Hover/Click Item`] --> [`Check Modifiers`] --+
                                                                         |
+------------------------------------------------------------------------+
|
+-> [`Toggle Allowance Command`] -> [`Update StorageSettings`] -> [`Overlay Color Change`]
|
+-> [`(Optional) Set Quick Search Command`] -> [`Update ITab_Storage Search`]
|
+-> [`(Optional) Open Storage Tab Command`] -> [`Focus ITab_Storage`]
```

### Interaction and Hotkeys

Requires pressing `PressR_ModifierKey` (default `R`) for all interactions.

**Click (`LMB`):**

- `PressR_ModifierKey` + `LMB` on `Item`: Toggles the allowance (`Allow`/`Disallow`) for that specific `Item` (`ThingDef`).
- `PressR_ModifierKey` + `ModifierIncrement_10x` + `LMB` on `Item`: Toggles allowance for the entire `Item` category (`ThingCategoryDef`).
- `PressR_ModifierKey` + `ModifierIncrement_100x` + `LMB` on `Item`: Toggles allowance for the parent category of the `Item`.
- `PressR_ModifierKey` + `ModifierIncrement_10x` + `ModifierIncrement_100x` + `LMB` on `Item`: Toggles allowance for **all** `Item`s (equivalent to "Allow All" / "Disallow All").

**Hover:** (If the `FocusItemInTabOnHover` option is enabled)

When hovering over an `Item`, the text in the `ITab_Storage` quick search field is updated:
- Without additional modifiers: Searches for the `Item` itself.
- `ModifierIncrement_10x`: Searches for the `Item`'s category.
- `ModifierIncrement_100x`: Searches for the `Item`'s parent category.
- `ModifierIncrement_10x` + `ModifierIncrement_100x`: Clears the search.

### Additional Options

- **`FocusItemInTabOnClick`**: If enabled (and `FocusItemInTabOnHover` is disabled), clicking on an `Item` also updates the text in the `ITab_Storage` quick search field (similar to the hover logic).
- **`openStorageTabAutomatically`**: If enabled, interaction (click or hover, depending on other settings) automatically opens `ITab_Storage`.
- **`restoreUIStateOnDeactivate`**: If enabled, when `StorageLens` deactivates (e.g., when deselecting the storage), the previous UI state is restored: selected object, open inspector tab, search text, and scroll position in `ITab_Storage`.


