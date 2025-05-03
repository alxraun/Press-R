# RimWorld GUI / IMGUI System Research

## 1. Core Concepts & Overview

*   **GUI vs IMGUI:** RimWorld uses Unity's **IMGUI** (Immediate Mode GUI) system. UI elements are defined and drawn immediately within the `OnGUI` loop using classes like `Widgets`. Layout is typically handled manually using `Rect` or helpers like `Listing_Standard`. State is not retained in UI objects but managed externally.
*   **Main Rendering Flow:** Unity calls `UIRoot.UIRootOnGUI()`, which in turn calls the specific implementation (`UIRoot_Play` or `UIRoot_Entry`). The key step for our focus is the call to `WindowStack.WindowStackOnGUI()`, which renders all active windows.

    ```mermaid
    graph TD
        A[Unity Engine] -->|Calls| B(MonoBehaviour.OnGUI);
        B --> C(Current UIRoot);
        C -- In Game --> D(UIRoot_Play.UIRootOnGUI);
        C -- Main Menu --> E(UIRoot_Entry.UIRootOnGUI);
    
        subgraph UIRoot_Play OnGUI Order
            D --> D1[base.UIRootOnGUI()];
            D1 --> D2[Game Info / World UI];
            D2 --> D3[Map UI (Before Tabs)];
            D3 --> D4[Main Buttons / Alerts];
            D4 --> D5[Map UI (After Tabs)];
            D5 --> D6[Tutor / Widgets (Pre-Windows)];
            D6 --> D7[**WindowStack.WindowStackOnGUI()**];
            D7 --> D8[Widgets (Post-Windows)];
            D8 --> D9[Debug Tools / Input Handling];
        end
    
        subgraph UIRoot_Entry OnGUI Order
            E --> E1[base.UIRootOnGUI()];
            E1 --> E2[... Entry Specific UI ...];
            E2 --> E3[**WindowStack.WindowStackOnGUI()**];
            E3 --> E4[...];
        end
    
        D7 --> F(WindowStack Draws Windows);
        E3 --> F;
        F --> G[Window.WindowOnGUI -> Window.InnerWindowOnGUI];
        G --> H[Window.DoWindowContents using Widgets];
    
    ```

    ```ASCII
    [`Unity Engine`] --> [`MonoBehaviour.OnGUI`] --> [`UIRoot.*.UIRootOnGUI`]
                                                         |
                     +-----------------------------------+
                     |                                   |
          [`UIRoot_Play.UIRootOnGUI`]         [`UIRoot_Entry.UIRootOnGUI`]
                     |                                   |
          [... Draw HUD Elements ...]         [... Draw Menu Elements ...]
                     |                                   |
          [`WindowStack.WindowStackOnGUI()`] <--+---------+
                     |
          [... Debug Tools / Input ...]
    
    [`WindowStack.WindowStackOnGUI()`]
          |-- Loops through active `Window`s
          +-- Calls `Window.WindowOnGUI()` for each
                |-- Calls `Window.InnerWindowOnGUI()` (Unity's `GUI.Window` callback)
                      |-- Calls `Window.DoWindowContents()` (Abstract method implemented by specific window class)
                            |-- Uses `Widgets`, `Listing_Standard`, etc. to draw content
    ```

## 2. Entry Point & Main Loop (`UIRoot`, `Root_Play`, `Root_Entry`)

*   **Initialization (`UIRoot.Init`)**
    *   Base `UIRoot.Init()` is empty.
    *   `UIRoot_Play.Init()`: Clears `Messages`, may toggle debug palette.

*   **`UIRoot.UIRootOnGUI()` Breakdown (Base operations, called first)**
    1.  Debug/System Stuff: `DebugInputLogger`, `UnityGUIBugsFixer`, `SteamDeck` support.
    2.  Input Recording: `OriginalEventUtility.RecordOriginalEvent`.
    3.  Text Init: `Text.StartOfOnGUI()`.
    4.  Window Open Requests: `CheckOpenLogWindow`, `DelayedErrorWindowRequest`.
    5.  Debug Tools UI: `debugWindowOpener.DevToolStarterOnGUI`.
    6.  High Priority Window Events: `windows.HandleEventsHighPriority()`.
    7.  Screenshot Mode Handling: `screenshotMode.ScreenshotModesOnGUI`.
    8.  Core IMGUI Helpers (if not screenshot mode): `TooltipHandler`, `feedbackFloaters`, `DragSliderManager`, `Messages`.
    9.  Shortcuts: `shortcutKeys.ShortcutKeysOnGUI`.
    10. Debug UI: `NoiseDebugUI`, `CellInspectorDrawer`.
    11. Game Components: `GameComponentUtility.GameComponentOnGUI`.
    12. Event Reset: `OriginalEventUtility.Reset`.

*   **`UIRoot_Play.UIRootOnGUI()` Order of Operations (In-Game, called after base)**
    1.  `base.UIRootOnGUI()`.
    2.  Game Info / World UI.
    3.  Map UI (Colonist Bar, Resources, etc.): `mapUI.MapInterfaceOnGUI_BeforeMainTabs`.
    4.  Main Buttons & Alerts (if not screenshot mode).
    5.  Map UI (Inspect Pane, Gizmos): `mapUI.MapInterfaceOnGUI_AfterMainTabs`.
    6.  Tutor (if not screenshot mode).
    7.  Drag/Reorder Widgets (Before Windows).
    8.  **Window Rendering: `windows.WindowStackOnGUI()`**. 
    9.  Drag/Reorder Widgets (After Windows).
    10. Final Widgets Update: `Widgets.WidgetsOnGUI()`.
    11. Input Handling: Map Clicks, Designator Input, Debug Tools Input, Low Priority Shortcuts/Input.
    12. Escape Key Handling: `OpenMainMenuShortcut`.

*   **`UIRoot_Entry.UIRootOnGUI()` Order of Operations (Main Menu, called after base)**
    1.  `base.UIRootOnGUI()`.
    2.  World UI (if world exists).
    3.  Main Menu Drawing: `DoMainMenu()` -> `MainMenuDrawer.MainMenuOnGUI()`.
    4.  Tutor (if game exists).
    5.  Drag/Reorder Widgets (Before Windows).
    6.  **Window Rendering: `windows.WindowStackOnGUI()`**.
    7.  Drag/Reorder Widgets (After Windows).
    8.  Final Widgets Update: `Widgets.WidgetsOnGUI()`.
    9.  Low Priority Input (World).

*   **`UIRoot.UIRootUpdate()` (Base updates, called every frame)**
    *   Updates `ScreenshotTaker`, `DragSliderManager`.
    *   **Calls `windows.WindowsUpdate()` -> `Window.WindowUpdate()` for all windows.**
    *   Updates `MouseoverSounds`, `UIHighlighter`, `Messages`, `CellInspectorDrawer`.

*   **`UIRoot_Play.UIRootUpdate()` (In-Game)**
    *   Calls `base.UIRootUpdate()`.
    *   Updates World UI, Map UI, Alerts, Tutor/Lessons.

*   **`UIRoot_Entry.UIRootUpdate()` (Main Menu)**
    *   Calls `base.UIRootUpdate()`.
    *   Updates World UI, Tutor/Lessons (if game exists).

## 3. Window System (`WindowStack`, `Window`)

*   **`WindowStack` Management:**
    *   **Storage:** Holds a `List<Window> windows`.
    *   **Adding/Removing:** Windows are added via `WindowStack.Add(window)`. They are removed via `WindowStack.TryRemove(windowOrType, doCloseSound)`. `TryRemove` calls the window's `PreClose`/`PostClose` methods.
    *   **Ordering & Layers:** Windows are inserted into the list based on their `WindowLayer` enum (`GameUI` < `Dialog` < `Subdialog` < `Superimposed` < `Immediate`). Windows with higher layers are drawn last (on top) and receive input first. `Dialog` is the most common layer.
    *   **Focus & Input (`GetsInput`, `focusedWindow`, `absorbInputAroundWindow`):**
        *   `GetsInput(window)` determines if a window receives input. Typically, only the topmost window (`windows[windows.Count - 1]`) receives input, unless a window below it has `absorbInputAroundWindow = true`.
        *   Clicking inside a window (`Notify_ClickedInsideWindow`) brings it to the front within its layer and sets it as `focusedWindow`.
        *   `focusedWindow` handles `Accept`/`Cancel` key presses if `closeOnAccept`/`closeOnCancel` are true (via `Window.OnAcceptKeyPressed`/`OnCancelKeyPressed`). Focus can also be set manually (`Notify_ManuallySetFocus`).
        *   `absorbInputAroundWindow = true` prevents mouse clicks from passing through the window to elements underneath.
    *   **Update Loop (`WindowsUpdate`):** Called by `UIRoot`, iterates through all windows and calls their `WindowUpdate()` method.
    *   **Drawing Loop (`WindowStackOnGUI`):** Called by `UIRoot`.
        1.  Calls `ExtraOnGUI()` for all windows (top to bottom).
        2.  Updates and prepares Immediate Windows (see below).
        3.  Draws shadows if `drawShadow` is true.
        4.  Calls `WindowOnGUI()` for all windows (bottom to top), including Immediate Windows rendered via `GUI.Window`.
    *   **Closing Windows:** Windows can be closed via `X` button (`doCloseX`), a dedicated close button (`doCloseButton`), `Esc` key (`closeOnCancel`), `Enter` key (`closeOnAccept`), clicking outside (`closeOnClickedOutside`), or programmatically calling `window.Close()`.
    *   **Modality:** Achieved by using a high `WindowLayer` (like `Dialog`) combined with `absorbInputAroundWindow = true`. The `grayOutIfOtherDialogOpen` flag can visually indicate modality when another dialog is opened on top.
    *   **Resolution Changes:** Handles screen resolution changes via `AdjustWindowsIfResolutionChanged()`, which calls `Notify_ResolutionChanged()` on each window. Default `Window` behavior is to call `SetInitialSizeAndPosition()`.
    *   **Immediate Windows:** `WindowStack.ImmediateWindow(...)` provides a way to draw simple, temporary windows defined by an `Action` delegate instead of a full `Window` subclass. They are managed separately and don't use the full `Window` lifecycle (no `PreOpen`/`PostOpen` etc.). They are useful for things like tooltips or temporary popups generated directly in `OnGUI`.

*   **`Window` Lifecycle & Properties:**
    *   **Lifecycle:**
        1.  `Constructor`: Basic setup.
        2.  `PreOpen()`: Called by `WindowStack.Add` *before* adding to list. Sets initial size/position, notifies UI systems (selectors, draggers).
        3.  `PostOpen()`: Called by `WindowStack.Add` *after* adding to list. Plays sounds.
        4.  `WindowUpdate()`: Called every frame by `WindowStack`. For per-frame logic (e.g., maintaining sounds).
        5.  `ExtraOnGUI()`: Called by `WindowStack` *before* `WindowOnGUI`. Allows drawing outside the main window bounds.
        6.  `WindowOnGUI()`: Called by `WindowStack`. Creates the `UnityEngine.GUI.Window` which calls `InnerWindowOnGUI`.
        7.  `InnerWindowOnGUI()` (Internal callback): Handles standard window chrome (background, title, close button, drag/resize), manages input within the window, sets up the `Rect` for content, and calls `DoWindowContents`.
        8.  `DoWindowContents(Rect inRect)`: **Abstract.** Subclasses implement this to draw the window's actual content using `Widgets` etc. `inRect` is the content area inside the margins.
        9.  `OnCloseRequest()`: Called by `WindowStack.TryRemove`. Returns `true` if closing is allowed.
        10. `PreClose()`: Called by `WindowStack.TryRemove` *before* removing from list.
        11. `PostClose()`: Called by `WindowStack.TryRemove` *after* removing from list. Stops sounds.
    *   **Key Properties:**
        *   `layer`: `WindowLayer` enum, determines draw/input order.
        *   `doCloseX`, `doCloseButton`, `closeOnAccept`, `closeOnCancel`, `closeOnClickedOutside`: Flags controlling closing behavior.
        *   `forcePause`: Pauses the game clock.
        *   `preventCameraMotion`: Stops camera movement via mouse/keyboard.
        *   `draggable`, `resizeable`: Allow user interaction.
        *   `absorbInputAroundWindow`: Makes the window modal-like regarding input.
        *   `optionalTitle`: Text in the title bar.
        *   `InitialSize`: Default size.
        *   `Margin`: Standard padding inside the window frame.
        *   `soundAppear`, `soundClose`, `soundAmbient`: Sounds associated with the window lifecycle.
        *   `drawInScreenshotMode`: Whether the window should be visible in screenshots.
        *   `onlyDrawInDevMode`: Restricts window visibility to dev mode.
    *   **Custom Drawing (`IWindowDrawing`):** The constructor accepts an optional `IWindowDrawing` interface (defaults to `DefaultWindowDrawing`) to customize how the window background and chrome are drawn.
    *   **Message Background (`CausesMessageBackground()`):** Virtual method to indicate if this window should trigger the semi-transparent message background (default `false`).

*   **Modal Windows:** Typically achieved using `WindowLayer.Dialog` (or higher) and `absorbInputAroundWindow = true`.
*   **Standard Window Types (`Dialog_*`, `FloatMenu`):** Many subclasses exist, like `Dialog_MessageBox`, `Dialog_Options`, `Dialog_InfoCard`, `Dialog_FileList`. `FloatMenu` is also a `Window`.

## 4. Input Handling (`Event`, `KeyBindingDef`, `Mouse`)

*   **`Event.current` Usage:**
    *   The core of IMGUI input handling relies on `UnityEngine.Event.current` within the `OnGUI` loop.
    *   It provides information about the current input event: type (`EventType.KeyDown`, `MouseDown`, `Repaint`, etc.), key (`keyCode`), mouse button (`button`), mouse position (`mousePosition`), and modifiers (`shift`, `control`, `alt`, `command`).
    *   Checked repeatedly by windows, widgets, and other UI systems to react to user actions.
    *   **Crucially, `Event.current` only holds relevant data for the specific event type being processed in `OnGUI`. You cannot reliably check `Event.current.keyCode` during an `EventType.MouseDown`, for example.**

*   **Input Consumption (`Event.current.Use()`):**
    *   Essential for preventing the same input event from being processed by multiple UI elements.
    *   When a UI element (widget, window, tool) handles an event (like a mouse click or key press), it calls `Event.current.Use()`.
    *   This marks the event as consumed, and subsequent checks for this event in the same `OnGUI` cycle will typically ignore it.
    *   Widely used throughout the UI code (`Widgets`, `Window`, `WindowStack`, `DesignatorManager`, shortcut handlers).

*   **Keyboard Input (`KeyBindingDef`, `KeyBindingDefOf`):**
    *   RimWorld uses `KeyBindingDef` (a `Def`) to define abstract user actions (e.g., "Cancel", "Pause", "Rotate Left") and map them to default keyboard keys (`defaultKeyCodeA`, `defaultKeyCodeB`).
    *   Users can rebind these keys via `KeyPrefs`.
    *   `KeyBindingDefOf` provides static access to common vanilla keybindings.
    *   The primary way to check for key presses related to actions in `OnGUI` is the `KeyDownEvent` property (e.g., `if (KeyBindingDefOf.Cancel.KeyDownEvent)`). This checks if `Event.current.type == EventType.KeyDown` and the `keyCode` matches one of the bound keys for that `KeyBindingDef`.
    *   Other properties like `IsDown` (`Input.GetKey`) and `JustPressed` (`Input.GetKeyDown`) exist but are less common for immediate UI reactions within `OnGUI`.

*   **Mouse Interaction (`Mouse`, `Widgets`):**
    *   Mouse events are typically checked via `Event.current.type == EventType.MouseDown` or `EventType.MouseUp`, along with `Event.current.button` (0=left, 1=right, 2=middle).
    *   Standard controls (`Widgets`) encapsulate mouse handling:
        *   They check if the mouse is over their `Rect` (`rect.Contains(Event.current.mousePosition)`).
        *   They check if input is blocked (`!Mouse.IsInputBlockedNow`).
        *   They react to `MouseDown`/`MouseUp` events within their bounds.
        *   They return `true` if interaction occurred (e.g., button clicked).
        *   They call `Event.current.Use()` upon successful interaction.
    *   `Mouse.IsInputBlockedNow` prevents interaction if:
        *   The mouse is over an inactive `ScrollView`.
        *   The mouse is obscured by a window (`WindowStack.MouseObscuredNow`).
        *   The current window shouldn't receive input (`!WindowStack.CurrentWindowGetsInput`).
    *   `Mouse.IsOver(Rect rect)` combines the `Contains` check with `!Mouse.IsInputBlockedNow`.

*   **Input Processing Order & Low Priority Input:**
    *   Input is generally processed in a specific order within `UIRoot_Play.UIRootOnGUI`:
        1.  High-priority events handled by `WindowStack` (clicks outside windows, global Cancel/Accept if no window focused).
        2.  Window-specific input handled within `Window.InnerWindowOnGUI` (driven by `WindowStack.WindowStackOnGUI`). The topmost window capable of receiving input gets priority.
        3.  Active `Designator` input (`DesignatorManager.SelectedDesignator.SelectedProcessInput`).
        4.  Low-priority global shortcuts (`MainButtonsRoot.HandleLowPriorityShortcuts`, `ShortcutKeys.ShortcutKeysOnGUI`).
        5.  Low-priority map/world input (`MapInterface.HandleMapClicks`, `MapInterface.HandleLowPriorityInput`, `WorldInterface.HandleLowPriorityInput`).
    *   If an event is consumed (`Event.current.Use()`) at a higher priority level (e.g., by a window or designator), lower priority handlers will ignore it.

## 5. IMGUI Primitives (`Widgets`, `GUI`, `GenUI`)

*   **Core Drawing Functions (`Widgets`, `UnityEngine.GUI`):**
    *   **`UnityEngine.GUI`:** The base Unity IMGUI API. Provides low-level functions:
        *   `GUI.DrawTexture`, `GUI.DrawTextureWithTexCoords`: Basic texture drawing.
        *   `GUI.Label`, `GUI.Button`, `GUI.TextField`, `GUI.TextArea`: Basic controls (usually wrapped by `Widgets`).
        *   `GUI.BeginGroup`/`EndGroup`: Clipping regions.
        *   `GUI.Window`: Creates the window container used by `Verse.Window`.
        *   `GUI.DragWindow`: Enables dragging for `GUI.Window`.
    *   **`Verse.Widgets`:** The primary RimWorld UI drawing class. Contains wrappers and custom implementations:
        *   `DrawBox`, `DrawLine`, `DrawBoxSolid`: Drawing shapes.
        *   `DrawTextureFitted`, `DrawTexturePart`, `DrawTextureRotated`: Advanced texture drawing.
        *   `DrawWindowBackground`, `DrawMenuSection`: Standard backgrounds.
        *   `DrawHighlight`, `DrawHighlightIfMouseover`: Highlighting effects.
        *   Wraps most standard controls (see below).

*   **Standard Controls (`Widgets`):**
    *   `Label`, `LabelScrollable`, `LabelEllipses`, `LabelWithIcon`: Text display.
    *   `ButtonText`, `ButtonImage`, `ButtonInvisible`, `ButtonTextSubtle`, `CloseButtonFor`: Various button types.
    *   `Checkbox`, `CheckboxLabeled`, `CheckboxMulti`: Checkboxes.
    *   `RadioButton`, `RadioButtonLabeled`: Radio buttons.
    *   `TextField`, `TextArea`, `TextFieldNumeric`, `TextFieldPercent`: Text and number input fields.
    *   `HorizontalSlider`: Slider control.
    *   `FloatRange`, `IntRange`, `QualityRange`: Range selection controls.
    *   `FillableBar`, `FillableBarLabeled`: Progress/filled bars.
    *   `InfoCardButton`: Standard "i" button.
    *   `Dropdown`: Dropdown menu control.
    *   `BeginScrollView`, `EndScrollView`: Scrollable areas.
    *   `ThingIcon`, `DefIcon`: Displaying icons for Things and Defs.

*   **State Management (`UnityEngine.GUI`, `Verse.Text`):**
    *   IMGUI relies heavily on immediate state changes.
    *   **`GUI` Static Properties:** Control global drawing state:
        *   `GUI.color`: Primary tint color for textures/elements.
        *   `GUI.contentColor`: Color for text.
        *   `GUI.backgroundColor`: Background color for some elements.
        *   `GUI.skin`: Defines default styles (`GUISkin`). RimWorld uses a standard skin, customized mainly via `Text`.
        *   `GUI.matrix`: Transformation matrix (used for UI scaling).
        *   `GUI.depth`: Drawing order.
    *   **`Text` Static Properties:** Control text rendering state:
        *   `Text.Font`: Current font size (`GameFont` enum: Tiny, Small, Medium).
        *   `Text.Anchor`: Current text alignment (`TextAnchor` enum).
    *   **Common Practice:** Save the current state (e.g., `Color originalColor = GUI.color;`), change it, draw the element(s), then restore the original state (`GUI.color = originalColor;`). This is crucial for `GUI.color`, `Text.Font`, and `Text.Anchor`.

*   **Utility Functions (`GenUI`):**
    *   Provides helper functions for common UI tasks.
    *   `Rect` manipulation extensions (`ContractedBy`, `ExpandedBy`, `LeftPart`, `RightPart`, `SplitHorizontally`, etc.).
    *   `GetSizeCached`, `GetWidthCached`, `GetHeightCached`: Cached text size calculation (wraps `Text.CalcSize`).
    *   `DrawMouseAttachment`: Drawing icons/text attached to the mouse cursor.
    *   `TargetsAtMouse`, `ThingsUnderMouse`: Finding UI elements or game objects under the mouse.
    *   `DrawTextWinterShadow`: Special effect for text shadow.
    *   `DrawElementStack`: Arranging elements dynamically in rows/columns.

## 6. Layout Systems (`Rect`, `Listing_Standard`, `Listing_Tree`, `WidgetRow`)

*   **`UnityEngine.Rect` Usage and Manipulation:**
    *   The fundamental structure for defining position and size (`x`, `y`, `width`, `height`).
    *   Manual calculation and manipulation of `Rect` values is the core of IMGUI layout.
    *   **`GenUI` Extensions:** Provide numerous crucial helper methods for `Rect` manipulation, making layout calculations much cleaner (e.g., `rect.ContractedBy(margin)`, `rect.ExpandedBy(margin)`, `rect.LeftPart(pct)`, `rect.RightPartPixels(width)`, `rect.TopHalf()`, `rect.SplitHorizontally(...)`, `rect.SplitVertically(...)`, `rect.CenteredOnXIn(...)`, `rect.AtZero()`). These are used extensively.

*   **`Listing_Standard` for Vertical Layouts:**
    *   A helper class (`Verse.Listing`) designed to simplify creating vertical lists of standard UI elements.
    *   **Workflow:**
        1.  `var listing = new Listing_Standard();`
        2.  `listing.Begin(rect);` (Defines the total area for the listing)
        3.  Call methods like `listing.Label(...)`, `listing.ButtonText(...)`, `listing.CheckboxLabeled(...)`, `listing.Slider(...)`, `listing.TextFieldNumeric(...)`, etc.
        4.  Each method automatically calculates the required height, draws the widget using `Verse.Widgets`, and advances the internal vertical position (`curY`) by the element's height plus `listing.verticalSpacing`.
        5.  `listing.Gap(height)`: Adds a specific vertical gap.
        6.  `listing.GetRect(height)`: Reserves a `Rect` of the specified height, advances `curY`, and returns the `Rect` for custom drawing within it.
        7.  `listing.NewColumn()`: Moves to the next column (if width allows).
        8.  `listing.End();`
    *   **Scrolling:** Can be initialized with a bounding rectangle and a scroll position getter. It will then cull elements outside the visible area for performance. Note that `Widgets.BeginScrollView`/`EndScrollView` are used *around* the `Listing_Standard`'s rect.
    *   **Sections:** `listing.BeginSection(height)` / `listing.EndSection(nestedListing)` allow creating nested sections with standard backgrounds/margins.
    *   Widely used for settings windows, dialogs, inspect panes, etc.

*   **`Listing_Tree` for Hierarchical Views:**
    *   A helper class (`Verse.Listing_Lines`) for indented, tree-like structures (e.g., debug views, def editors).
    *   Manages indentation levels (`indentLevel`, `nestIndentWidth`).
    *   Provides methods like `LabelLeft` (draws label with indent) and `OpenCloseWidget` (draws +/- button for `TreeNode`).
    *   Sets `Text.Anchor = TextAnchor.MiddleLeft` and `Text.WordWrap = false` by default.
    *   Used less frequently than `Listing_Standard`.

*   **`WidgetRow` for Horizontal Layouts:**
    *   A helper class (`Verse.WidgetRow`) for arranging elements **horizontally** within a single line.
    *   **Workflow:**
        1.  `var row = new WidgetRow(startX, startY, direction = UIDirection.RightThenDown, maxWidth = ..., gap = ...);`
        2.  Call methods like `row.Icon(...)`, `row.ButtonIcon(...)`, `row.Label(...)`, `row.Gap(...)`.
        3.  Each method draws the element at the current horizontal position and advances the position for the next element, considering the specified `gap`.
    *   Often used *within* a `Rect` obtained from `Listing_Standard.GetRect()` or inside custom drawing code to place multiple small controls (like icons or small buttons) side-by-side.

## 7. Text & Localization (`Text`, `TaggedString`, `Translator`)

*   **`Text` Class (Static):**
    *   Manages global text rendering state: `Text.Font` (`GameFont`), `Text.Anchor` (`TextAnchor`), `Text.WordWrap` (`bool`).
    *   State must be saved before drawing and restored after (e.g., `GameFont originalFont = Text.Font; Text.Font = newFont; Widgets.Label(...); Text.Font = originalFont;`).
    *   Provides pre-configured `GUIStyle` via `Text.CurFontStyle`, `Text.CurTextFieldStyle`, etc., based on current static properties.
    *   Calculates text size ignoring markup: `Text.CalcHeight(string text, float width)`, `Text.CalcSize(string text)`. Uses `string.StripTags()` internally.
    *   Resets state to defaults at the start of each GUI frame via `Text.StartOfOnGUI()`.

*   **`Translator` & `string.Translate()`:**
    *   `string.Translate()` is the primary extension method for localization.
    *   Looks up the string key in the active language's `Keyed` files (`LoadedLanguage.keyedReplacements`). Falls back to the default language (English) if not found. Returns the key itself if missing entirely.
    *   Returns a `TaggedString`.
    *   **Arguments:**
        *   **Named (Preferred):** `Translate(NamedArgument arg1, ...)` (via `TranslatorFormattedStringExtensions`). Uses `{KeyName}` format in XML/strings. Example: `"MyKey".Translate(NamedArgumentUtility.Named(pawn, "PAWN"))`. Internally calls `.Formatted()` on the resulting `TaggedString`.
        *   **Indexed (Obsolete):** `Translate(params object[] args)`. Uses `{0}`, `{1}` format. Marked `[Obsolete]`. Uses `string.Format` after translation.

*   **`TaggedString`:**
    *   A `struct` wrapper around a `string` (`RawText`). Returned by `Translate()`.
    *   Purpose: To preserve potential formatting tags (like `<color=red>...</color>`, `<b>...</b>`) from the localization files.
    *   **Implicit Conversion to `string`:** Automatically calls `RawText.StripTags()`, removing all formatting tags. This happens when passing a `TaggedString` to methods expecting `string` (like `Widgets.Label`).
    *   Provides text manipulation methods (`CapitalizeFirst`, `Replace`, `Trim`, etc.) that operate on the `RawText` while preserving tags where possible.
    *   `Resolve()` uses `ColoredText.Resolve` to potentially process tags for rendering.

*   **Localization Files (`Languages` folder):**
    *   **`Keyed/`:** XML files (`<LanguageData><MyKey>My Translation {PAWN_name}</MyKey>...</LanguageData>`) for C# strings accessed via `Translate()`.
    *   **`DefInjected/`:** XML files mirroring Def structure for translating Def fields (e.g., `<ThingDef><Beer.label>Beer</Beer.label>...</ThingDef>`). Applied automatically by `DefInjectionPackage`.
    *   **`Strings/`:** Lists of words/phrases used by `RulePackDef` for procedural text generation. Accessed via `Translator.TryGetTranslatedStringsForFile` (used by `Rule_File`).

## 8. Standard UI Components (Within Windows)

### 8.1 Floating Menus (`FloatMenu`, `FloatMenuOption`)

*   **`FloatMenu`:**
    *   A specialized `Window` used for context menus or selection lists.
    *   Created by passing a `List<FloatMenuOption>`: `Find.WindowStack.Add(new FloatMenu(options));`.
    *   Automatically sorts options by `MenuOptionPriority` (descending) then `orderInPriority` (descending).
    *   Calculates size and column layout dynamically to fit options, using a `ScrollView` if necessary.
    *   Appears near the mouse cursor.
    *   No standard window background or shadow.
    *   Closes when clicking outside or selecting an option.
    *   Can have an optional `title` displayed above.

*   **`FloatMenuOption`:**
    *   Represents a single selectable item in a `FloatMenu`.
    *   **Key constructor parameters:**
        *   `label` (string): Display text.
        *   `action` (Action): Code executed when the option is chosen. If `null`, the option is disabled.
        *   `priority` (MenuOptionPriority): For sorting.
        *   `mouseoverGuiAction` (Action<Rect>): Optional GUI drawn when hovering.
        *   `revalidateClickTarget` (Thing/WorldObject): If set, the option becomes disabled if this target is invalid/destroyed.
        *   `extraPartWidth` / `extraPartOnGUI`: Allows drawing custom interactive elements on the right side.
    *   Can display an `icon` (from ThingDef, Texture2D, or Thing instance) to the left of the label.
    *   Handles drawing its background (highlighting on mouseover), icon, label, and optional extra part.
    *   Calls the `action` and closes the menu upon successful click.

*   **Creation and Display Flow:**
    1.  Code identifies the need for a menu (e.g., right-click).
    2.  A `List<FloatMenuOption>` is populated, each option configured with a label, action, etc.
    3.  A new `FloatMenu` instance is created with the list: `var menu = new FloatMenu(options);`.
    4.  The menu is added to the window stack: `Find.WindowStack.Add(menu);`.
    5.  `FloatMenu` handles layout, drawing (calling `option.DoGUI()` for each), and input.
    6.  When an option is clicked, its `action` is executed, and the `FloatMenu` is closed.

## 9. UI Assets & Styling (`TextureAtlasGroup`, `UIContentPack`, `GUI.skin`)

*   **Loading UI Textures**
*   **Texture Atlases**
*   **`GUI.skin` Usage (Limited in RimWorld?)**

## 10. UI Scaling (`Prefs.UIScale`, `UI.CurUICellSize`)

*   **`Prefs.UIScale`:**
    *   A user-configurable setting (float) that determines the global scaling factor for the UI.
    *   Managed in `Prefs.cs` and `PrefsData.cs`.
    *   Read by `Verse.UI` class.

*   **`Verse.UI` Class Integration:**
    *   `UI.ApplyUIScale()`:
        *   Calculates scaled screen dimensions: `UI.screenWidth = Screen.width / Prefs.UIScale;` and `UI.screenHeight = Screen.height / Prefs.UIScale;`.
        *   Sets the global `GUI.matrix` to `Matrix4x4.Scale(new Vector3(Prefs.UIScale, Prefs.UIScale, 1f))`. This matrix automatically scales all subsequent IMGUI drawing calls.
    *   Coordinate Conversions: Provides methods like `MousePositionOnUI`, `MapToUIPosition`, `GUIToScreenPoint` which incorporate `Prefs.UIScale` in their calculations.

*   **How Scaling Affects Layouts:**
    *   **Automatic Scaling:** Due to the `GUI.matrix`, all standard `Rect` coordinates and sizes defined in UI space are automatically scaled visually.
    *   **No Manual Multiplication:** You generally **do not** need to manually multiply `Rect` dimensions or widget sizes by `Prefs.UIScale`. Define your layout using the scaled coordinates provided by `UI.screenWidth`, `UI.screenHeight`.
    *   **Layout Helpers:** Classes like `Listing_Standard` and `WidgetRow` operate within the scaled UI coordinate system defined by the `Rect` they are given. They don't need explicit knowledge of `Prefs.UIScale` themselves.
    *   **Resolution Utility:** `ResolutionUtility.GetRecommendedUIScale()` provides a suggested scale based on screen resolution. `ResolutionUtility.UIScaleSafeWithResolution()` checks if a scale is considered appropriate for a given resolution.

*   **`UI.CurUICellSize()`:**
    *   Returns the current visual size (width/height in UI pixels) of a single map tile.
    *   This value is dynamic, affected by both the current camera zoom level *and* `Prefs.UIScale`.
    *   Useful for UI elements that need to align precisely with the map grid (e.g., drawing overlays directly over cells), but less relevant for general window/widget layout.

*   **Adapting Custom UI:**
    *   Standard practice is sufficient: Define layouts using `Rect` based on `UI.screenWidth`/`Height` and use standard `Widgets` or layout helpers.
    *   Avoid hardcoding pixel values that assume a scale of 1.0.
    *   Use relative positioning and standard margins/padding where possible.
    *   Test your UI with different `Prefs.UIScale` values (especially non-integer ones if supported, although the UI allows only specific steps like 1x, 1.1x etc.) to ensure elements don't overlap or become misaligned.

## 11. UI Sounds (`SoundDefOf`, `MouseoverSounds`)

*   **Assigning Sounds to Widgets:**
    *   **Click/Interaction Sounds (`SoundDefOf`):**
        *   Sounds for direct interactions (button clicks, checkbox toggles) are typically played directly within the widget's logic (e.g., inside `Widgets.ButtonText` or `Widgets.CheckboxLabeled`).
        *   When an interaction condition is met (e.g., `MouseUp` event within the widget's `Rect`), the relevant `SoundDef` from `SoundDefOf` (like `SoundDefOf.Click` or `SoundDefOf.Checkbox_TurnedOn`) is played using `soundDef.PlayOneShotOnCamera()`.
        *   Specific widgets often hardcode their associated interaction sounds.
    *   **Mouseover Sounds (`MouseoverSounds`):**
        *   Managed by the static `MouseoverSounds` class.
        *   Widgets that should have a hover sound call `MouseoverSounds.DoRegion(widgetRect, SoundDefOf.Mouseover_Standard)` (or a custom `SoundDef`) during their drawing phase (`EventType.Repaint`).
        *   `MouseoverSounds.ResolveFrame()` (called once per frame) iterates through all regions registered in that frame.
        *   It plays the sound for the *first* region the mouse is currently over, but only if the mouse *just entered* that region (i.e., it wasn't over the same `Rect` in the previous frame). This prevents sound spamming.
    *   **Window Sounds:**
        *   `Window` has `soundAppear` (default `SoundDefOf.DialogBoxAppear`) and `soundClose` (default `SoundDefOf.Click`) properties.
        *   These are played automatically by `WindowStack` when the window is added (`PostOpen`) or removed (`PostClose`).

## 12. Debugging UI (`DebugTools`)

*   **Access:** Debug tools are accessed via the debug menu bar (`DebugWindowsOpener`) at the top of the screen when Dev Mode is enabled.

*   **Relevant Debug Windows & Actions:**
    *   **Inspector (`EditWindow_DebugInspector`):**
        *   Allows selecting and viewing detailed information about game objects (Things, Pawns, Terrain, etc.).
        *   Crucial for understanding the *data* that UI elements are trying to display, even if it doesn't inspect the UI widgets themselves.
    *   **View Settings (`Dialog_Debug` -> Settings Tab / `DebugViewSettings` class):
        *   Contains toggles for various rendering layers and debug overlays.
        *   UI-related options include:
            *   `drawTooltipEdges`: Shows boundaries for tooltips.
            *   `logInput`: Logs mouse/keyboard events.
            *   `showFloatMenuWorkGivers`: Adds debug info to work giver float menus.
            *   `drawWoundAnchorsOnHover`: Visualizes wound anchor points on health tab hover.
        *   Many options affect world rendering (`drawPaths`, `drawRegions`, `drawShadows`), which can be useful for debugging UI elements interacting with the world view.
    *   **Actions Menu (`Dialog_Debug` -> Actions Tab):
        *   **Time Control:** Actions like `Tick +1`, `Tick +10`, `Fast Forward` are useful for stepping through UI updates or animations.
        *   **State Changes:** Actions to spawn things, change pawn stats, trigger events, etc., indirectly test UI updates by changing the underlying game state.
        *   **Tweak Values (`EditWindow_TweakValues`):** Allows modifying internal static variables, potentially affecting UI behavior if it reads those values.
    *   **Log (`EditWindow_Log`):** Essential for viewing error messages or custom debug output related to UI logic.
    *   **Dev Palette (`Dialog_DevPalette`):** Allows pinning frequently used debug actions (including any state-changing ones useful for UI testing) for quick access.

*   **Overlays:**
    *   `CellInspectorDrawer` (always active in Dev Mode): Shows basic info about the cell/things under the mouse cursor.
    *   Various overlays enabled via View Settings (see above).

*   **Limitations:** There aren't specific tools to directly inspect the IMGUI `Rect` hierarchy, styles, or event consumption in the same way a web inspector works. Debugging often relies on inspecting game state (`DebugInspector`) and adding log messages.
