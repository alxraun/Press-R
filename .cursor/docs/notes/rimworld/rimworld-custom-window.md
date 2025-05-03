# Creating Custom Window UI in RimWorld

## 1. Introduction

*   Focus on the classes used directly for building the UI elements *inside* a `Verse.Window`.

## 2. `Verse.Window` (Base & Drawing Entry Point)

This is the base class for all floating windows in the game.

*   **Inheritance:** Your custom window class must inherit from `Verse.Window`.

*   **Core Drawing Method:**
    *   `DoWindowContents(Rect inRect)` (`abstract void`): **Must be implemented.** This method receives the content area `Rect` (inside margins) and is where all UI elements for the window are drawn using `Widgets`, layout helpers, etc. Coordinates within `inRect` are relative to its top-left corner.

*   **Key Configuration Fields (Public `bool` Flags unless noted):**
    *   `layer` (`WindowLayer` enum): Determines draw order and focus behavior (e.g., `Dialog`, `GameUI`).
    *   `optionalTitle` (`string`): Text displayed in the title bar area.
    *   `doCloseX` (`bool`): Display the small 'X' close button in the top-right corner.
    *   `doCloseButton` (`bool`): Display the larger standard "Close" button at the bottom.
    *   `closeOnAccept` (`bool`): Close the window on the Accept key (e.g., Enter).
    *   `closeOnCancel` (`bool`): Close the window on the Cancel key (e.g., Escape).
    *   `closeOnClickedOutside` (`bool`): Close the window if the user clicks outside its bounds.
    *   `forcePause` (`bool`): Pause the game simulation while open.
    *   `preventCameraMotion` (`bool`): Prevent map camera movement via keyboard/mouse edge scroll.
    *   `doWindowBackground` (`bool`): Draw the standard textured window background.
    *   `absorbInputAroundWindow` (`bool`): Prevent mouse clicks outside the window rectangle from reaching underlying elements (acts like input modality).
    *   `resizeable` (`bool`): Allow user resizing via a corner handle.
    *   `draggable` (`bool`): Allow user dragging by the title bar/background.
    *   `drawShadow` (`bool`): Render a drop shadow.
    *   `focusWhenOpened` (`bool`): Attempt to gain input focus immediately upon opening.
    *   `soundAppear` / `soundClose` / `soundAmbient` (`SoundDef`): Sounds for window events.
    *   `grayOutIfOtherDialogOpen` (`bool`): Apply a gray overlay if another dialog opens on top.
    *   `preventSave` (`bool`): Prevent game saving while open.
    *   (And others like `forceCatchAcceptAndCancelEventEvenIfUnfocused`, `preventDrawTutor`, `onlyOneOfTypeAllowed`, `silenceAmbientSound`, `openMenuOnCancel`, `drawInScreenshotMode`, `onlyDrawInDevMode`).

*   **Key Properties:**
    *   `InitialSize` (`virtual Vector2`): Override to set the desired initial width and height of the window.
    *   `Margin` (`protected virtual float`): Override to change the padding between the window edge and the content area (`inRect`). Default is `18f`.
    *   `IsOpen` (`bool`): Read-only. Checks if the window is currently managed by `Find.WindowStack`.
    *   `windowRect` (`Rect`): The current position and size of the window on the screen. Managed internally but can be read.
    *   `ID` (`int`): Unique identifier assigned by `WindowStack`.

*   **Lifecycle & Update Methods (`virtual void`):**
    *   `PreOpen()`: Called just *before* the window is added to `WindowStack`. Use for initialization, setting `InitialSize`, etc.
    *   `PostOpen()`: Called just *after* the window is added. Default plays `soundAppear`, starts `soundAmbient`.
    *   `WindowUpdate()`: Called every frame while the window is open. Use for continuous logic (e.g., managing ambient sound).
    *   `ExtraOnGUI()`: Called every GUI frame *before* the main window drawing. Allows drawing outside the standard window bounds if needed.
    *   `OnCloseRequest()` (`virtual bool`): Called when closing is requested. Return `false` to prevent closing.
    *   `PreClose()`: Called just *before* the window is removed from `WindowStack`. Use for cleanup before closing.
    *   `PostClose()`: Called just *after* the window is removed. Default stops `soundAmbient`, plays `soundClose`.

*   **Input Handling Methods (`virtual void`):**
    *   `OnCancelKeyPressed()`: Called when the Cancel key is pressed and the window should handle it. Default implementation handles `closeOnCancel` and `openMenuOnCancel` flags.
    *   `OnAcceptKeyPressed()`: Called when the Accept key is pressed and the window should handle it. Default implementation handles `closeOnAccept` flag.

*   **Utility Methods:**
    *   `Close(bool doCloseSound = true)` (`virtual void`): Convenience method to request the window be closed via `Find.WindowStack.TryRemove`.
    *   `CausesMessageBackground()` (`virtual bool`): Override and return `true` if this window should trigger the semi-transparent background used for messages.
    *   `Notify_ResolutionChanged()` (`virtual void`): Called automatically on screen resolution changes. Default calls `SetInitialSizeAndPosition()`.
    *   `Notify_ClickOutsideWindow()` (`virtual void`): Called when the user clicks outside this window (and `closeOnClickedOutside` is false).

*   **Protected Helpers:**
    *   `SetInitialSizeAndPosition()` (`protected virtual void`): Calculates the initial `windowRect` based on `InitialSize` and centers it on the screen.

## 3. `Verse.Widgets` (UI Elements & Primitives)

This static class provides a vast collection of methods for drawing standard UI controls, basic shapes, textures, and handling common interaction patterns within the IMGUI system.

*   **Purpose:** The primary toolkit for rendering UI elements inside `DoWindowContents`.

*   **Text Display:**
    *   `Label(Rect rect, string label)` / `Label(Rect rect, TaggedString label)`: Draws standard text. Respects `Text` state.
    *   `LabelEllipses(Rect rect, string label)`: Draws text, adding "..." if it doesn't fit.
    *   `LabelScrollable(Rect rect, string label, ref Vector2 scrollbarPosition, ...)`: Draws text within a scrollable area.
    *   `LabelWithIcon(Rect rect, string label, Texture2D labelIcon, ...)`: Draws an icon followed by text.
    *   `DefLabelWithIcon(Rect rect, Def def, ...)`: Draws the Def's icon and label, often used in lists.
    *   `LongLabel(float x, float width, string label, ref float curY, ...)`: Efficiently draws very long text that might span multiple lines/pages.
    *   `NoneLabel(...)`: Draws a centered "(none)" label, usually indicating absence.

*   **Buttons:**
    *   `ButtonText(Rect rect, string label, ...)` (`bool`): Standard text button. Returns `true` when clicked.
    *   `ButtonTextDraggable(Rect rect, string label, ...)` (`Widgets.DraggableResult`): Text button that supports dragging. Returns result state (Idle, Pressed, Dragged, DraggedThenPressed).
    *   `ButtonImage(Rect butRect, Texture2D tex, ...)` (`bool`): Image button.
    *   `ButtonImageDraggable(Rect butRect, Texture2D tex, ...)` (`Widgets.DraggableResult`): Draggable image button.
    *   `ButtonImageFitted(Rect butRect, Texture2D tex, ...)` (`bool`): Image button, scales image to fit.
    *   `ButtonTextSubtle(Rect rect, string label, ...)` (`bool`): Button with a subtle background, often used in lists/tabs.
    *   `ButtonInvisible(Rect butRect, ...)` (`bool`): Invisible clickable area.
    *   `ButtonInvisibleDraggable(Rect butRect, ...)` (`Widgets.DraggableResult`): Invisible draggable area.
    *   `CloseButtonFor(Rect rectToClose)` (`bool`): Standard small 'X' close button.
    *   `BackButtonFor(Rect rectToBack)` (`bool`): Standard "Back" button (used less often than Close).
    *   `CustomButtonText(...)`: Allows creating buttons with custom background/border/text colors.

*   **Selection Controls:**
    *   `Checkbox(float x, float y, ref bool checkOn, ...)`: Basic checkbox.
    *   `CheckboxLabeled(Rect rect, string label, ref bool checkOn, ...)`: Checkbox with label.
    *   `CheckboxLabeledSelectable(Rect rect, string label, ref bool selected, ref bool checkOn, ...)`: List item that is selectable and has a checkbox.
    *   `CheckboxMulti(Rect rect, MultiCheckboxState state, ...)` (`MultiCheckboxState`): Checkbox with On/Off/Partial states.
    *   `RadioButton(float x, float y, bool chosen, ...)` (`bool`): Basic radio button. Returns `true` if clicked (caller manages state).
    *   `RadioButtonLabeled(Rect rect, string labelText, bool chosen, ...)` (`bool`): Radio button with label.

*   **Input Fields:**
    *   `TextField(Rect rect, string text)` (`string`): Single-line text input.
    *   `TextArea(Rect rect, string text, bool readOnly = false)` (`string`): Multi-line text input.
    *   `TextFieldNumeric<T>(Rect rect, ref T val, ref string buffer, ...)`: Input for numbers (int/float) with validation.
    *   `TextFieldPercent(Rect rect, ref float val, ref string buffer, ...)`: Numeric input for percentages (0-1).
    *   `IntEntry(Rect rect, ref int value, ref string editBuffer, ...)`: Numeric input with +/- buttons.
    *   `DelayedTextField(...)`: Text field where the value updates only on Enter or focus loss.

*   **Sliders & Range Controls:**
    *   `HorizontalSlider(Rect rect, ref float value, FloatRange range, ...)` / `HorizontalSlider(...)` (`float`): Standard horizontal slider.
    *   `FrequencyHorizontalSlider(Rect rect, float freq, ...)` (`float`): Slider specialized for frequency/time intervals.
    *   `FloatRange(Rect rect, int id, ref FloatRange range, ...)`: Control for selecting a min/max float range.
    *   `IntRange(Rect rect, int id, ref IntRange range, ...)`: Control for selecting a min/max int range.
    *   `QualityRange(Rect rect, int id, ref QualityRange range)`: Control for selecting a min/max Quality range.
    *   `FloatRangeWithTypeIn(...)`: Combines FloatRange slider with text fields for direct input.

*   **Progress/Fill Bars:**
    *   `FillableBar(Rect rect, float fillPercent, Texture2D fillTex, ...)`: Basic fillable bar.
    *   `FillableBarLabeled(Rect rect, float fillPercent, int labelWidth, string label)`: Bar with a label.
    *   `FillableBarChangeArrows(Rect barRect, float changeRate)` / `FillableBarChangeArrows(Rect barRect, int changeRate)`: Draws arrows next to a bar to indicate change.
    *   `DraggableBar(...)`: A fillable bar where the user can drag to set a target value.

*   **Scroll Views:**
    *   `BeginScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, ...)`: Starts a scrollable area.
    *   `EndScrollView()`: Ends the scrollable area.
    *   `ScrollHorizontal(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, ...)`: Helper to handle horizontal mouse wheel scrolling for a view.

*   **Drawing Primitives & Backgrounds:**
    *   `DrawBox(Rect rect, int thickness = 1, ...)`: Outline box.
    *   `DrawBoxSolid(Rect rect, Color color)`: Solid filled box.
    *   `DrawBoxSolidWithOutline(Rect rect, Color solidColor, Color outlineColor, ...)`: Filled box with outline.
    *   `DrawLineHorizontal(...)` / `DrawLineVertical(...)` / `DrawLine(...)`: Draws lines.
    *   `DrawWindowBackground(Rect rect)`: Standard window background.
    *   `DrawMenuSection(Rect rect)`: Standard background for sections within menus/windows.
    *   `DrawOptionUnselected(Rect rect)` / `DrawOptionSelected(Rect rect)` / `DrawOptionBackground(Rect rect, bool selected)`: Standard backgrounds for selectable list items.
    *   `BeginGroup(Rect rect)` / `EndGroup()`: Restricts drawing to a specific `Rect` area (clipping).

*   **Texture Drawing:**
    *   `DrawTextureFitted(Rect outerRect, Texture tex, float scale, ...)`: Draws texture scaled to fit.
    *   `DrawTextureRotated(Rect rect, Texture tex, float angle)`: Draws texture rotated.
    *   `DrawTexturePart(Rect drawRect, Rect uvRect, Texture2D tex)`: Draws a portion of a texture.
    *   `DrawAtlas(Rect rect, Texture2D atlas, ...)`: Draws a texture using 9-slicing (stretchy background).
    *   `DrawAtlasWithMaterial(Rect rect, Texture2D atlas, Material mat, ...)`: 9-slicing with a custom material.

*   **Icons:**
    *   `ThingIcon(...)`: Draws icon for a Thing or ThingDef.
    *   `DefIcon(...)`: Draws icon for various Def types.
    *   `DefLabelWithIcon(Rect rect, Def def, ...)`: Draws Def icon followed by its label.

*   **Highlighting:**
    *   `DrawHighlight(Rect rect)`: Standard faint highlight.
    *   `DrawHighlightIfMouseover(Rect rect)`: Highlight on mouse hover.
    *   `DrawLightHighlight(Rect rect)`: Very subtle highlight.
    *   `DrawStrongHighlight(Rect rect, Color? color = null)`: More prominent highlight box.
    *   `DrawTextHighlight(Rect rect, ...)`: Background highlight specifically for text.
    *   `DrawHighlightSelected(Rect rect)`: Standard highlight for a selected item.

*   **Color Controls:**
    *   `ColorSelectorIcon(Rect rect, Texture icon, Color color, ...)`: Displays a color swatch, potentially with an icon.
    *   `ColorBox(Rect rect, ref Color color, Color boxColor, ...)` (`bool`): A single clickable color box. Returns `true` if clicked, updates `ref color`.
    *   `ColorSelector(Rect rect, ref Color color, List<Color> colors, ...)`: Displays a grid of `ColorBox` options.
    *   `HSVColorWheel(Rect rect, ref Color color, ref bool currentlyDragging, ...)`: Interactive HSV color picker wheel.
    *   `ColorTemperatureBar(Rect rect, ref Color color, ref bool dragging, ...)`: Slider for picking color temperature.
    *   `ColorTextfields(...)`: Text fields for editing R/G/B or H/S/V values directly.

*   **Misc & Interaction Helpers:**
    *   `ListSeparator(ref float curY, float width, string label)`: Labeled horizontal separator line.
    *   `InfoCardButton(...)` (`bool`): Small 'i' button, opens info card when clicked.
    *   `Dropdown<Target, Payload>(...)`: Button that opens a `FloatMenu` for selection.
    *   `MouseAttachedLabel(string label, ...)`: Draws text attached to the mouse cursor.
    *   `DrawNumberOnMap(Vector2 screenPos, int number, Color textColor)`: Draws a number with background (often for debug).
    *   `DrawShadowAround(Rect rect)`: Draws the standard drop shadow effect around a rect.

## 4. `Verse.GenUI` (Rect Manipulation Helpers)

*   **Purpose:** Provides a collection of static utility methods, including many extension methods for `UnityEngine.Rect`, designed to simplify common layout calculations, drawing tasks, and interactions within the UI.

*   **Key `Rect` Extension Methods:**
    *   **Sizing & Margins:**
        *   `ContractedBy(float margin)` / `ContractedBy(float marginX, float marginY)`: Returns a new `Rect` shrunk by the specified margin(s).
        *   `ExpandedBy(float margin)` / `ExpandedBy(float marginX, float marginY)`: Returns a new `Rect` expanded by the specified margin(s).
        *   `ScaledBy(float scale)`: Returns a new `Rect` scaled relative to its center.
        *   `GetInnerRect()`: Convenience method, equivalent to `ContractedBy(17f)`.
    *   **Dividing & Partitioning:**
        *   `LeftHalf()`, `RightHalf()`, `TopHalf()`, `BottomHalf()`: Returns the corresponding half of the `Rect`.
        *   `LeftPart(float pct)`, `RightPart(float pct)`, `TopPart(float pct)`, `BottomPart(float pct)`: Returns a portion of the `Rect` based on a percentage (0.0 to 1.0).
        *   `LeftPartPixels(float width)`, `RightPartPixels(float width)`, `TopPartPixels(float height)`, `BottomPartPixels(float height)`: Returns a portion of the `Rect` based on a fixed pixel size, anchored to the corresponding edge.
    *   **Splitting with Calculation:**
        *   `SplitHorizontally(float topHeight, out Rect top, out Rect bottom)`: Splits the `Rect` horizontally, defining the top part's height.
        *   `SplitVertically(float leftWidth, out Rect left, out Rect right)`: Splits the `Rect` vertically, defining the left part's width.
        *   `SplitHorizontallyWithMargin(...)`, `SplitVerticallyWithMargin(...)`: More advanced versions that handle margins between the split parts and can report overflow if the specified sizes don't fit.
    *   **Positioning & Alignment:**
        *   `CenteredOnXIn(Rect otherRect)`, `CenteredOnYIn(Rect otherRect)`: Returns a new `Rect` with the same size, centered horizontally or vertically within `otherRect`.
        *   `AtZero()`: Returns a new `Rect` with the same size but positioned at (0, 0).
    *   **Utility:**
        *   `Corners()`: Returns an array of the `Rect`'s four corner points (`Vector2`).
        *   `Union(Rect b)`: Returns a new `Rect` that encompasses both the original `Rect` and `Rect b`.
        *   `Rounded()`: Returns a new `Rect` with integer coordinates (casting float values).

*   **Layout Helpers:**
    *   `DrawElementStack<T>(...)`: Arranges a list of elements horizontally within a given `Rect`, wrapping to new rows as needed. Requires delegates for drawing and getting the width of each element. Optimized for layout.
    *   `DrawElementStackVertical<T>(...)`: Arranges elements vertically, potentially in multiple columns if the first column fills up.
    *   `GetCenteredButtonPos(...)`: Calculates the X-coordinate for a button to center it horizontally within a group of buttons.

*   **Text & Label Utilities:**
    *   `SetLabelAlign(TextAnchor a)`, `ResetLabelAlign()`: Shortcuts to set/reset `Text.Anchor`.
    *   `GetSizeCached(string s)`, `GetWidthCached(string s)`, `GetHeightCached(string s)`: Calculates the size of a string using `Text.CalcSize` and caches the result for performance. Strips rich text tags before calculation.
    *   `ClearLabelWidthCache()`: Clears the label size cache.

*   **Drawing Utilities:**
    *   `DrawTextWinterShadow(Rect rect)`: Draws a subtle shadow behind text, intensity based on current map conditions (sun glow, snow).
    *   `DrawTextureWithMaterial(...)`, `DrawTexturePartWithMaterial(...)`: Allows drawing textures with a custom `Material`.
    *   `DrawFlash(...)`: Draws a flashing effect texture.
    *   `DrawMouseAttachment(...)`: Draws an icon and/or text attached to the mouse cursor, useful for drag-and-drop feedback.
    *   `RenderMouseoverBracket()`: Draws the standard square bracket overlay on the map cell under the mouse.
    *   `DrawArrowPointingAt(Rect rect)` / `DrawArrowPointingAtWorldspace(...)`: Draws an arrow at the edge of the screen pointing towards a target `Rect` or world position.

*   **Color Utilities:**
    *   `LerpColor(List<Pair<float, Color>> colors, float value)`: Linearly interpolates between colors in a list based on a value and corresponding thresholds.

*   **Mouse & Interaction:**
    *   `TargetsAtMouse(...)`, `TargetsAt(...)`: Finds game targets (Things, terrain) at the mouse position or a specific map position, considering `TargetingParameters`.
    *   `ThingsUnderMouse(...)`: Specifically finds `Thing` objects under the mouse cursor, with various checks for click radius and targeting parameters.
    *   `AbsorbClicksInRect(Rect r)`: Consumes mouse down events within the specified `Rect`, preventing clicks from passing through.
    *   `GetMouseAttachedWindowPos(float width, float height)`: Calculates a suitable top-left position for a tooltip-like window attached to the mouse, attempting to keep it on screen.

*   **Miscellaneous:**
    *   `CurrentAdjustmentMultiplier()`: Returns 1, 10, 100, or 1000 based on which modifier keys (Shift, Ctrl, Alt - depending on keybindings) are held down, used for adjusting values in UI controls.
    *   `IconDrawScale(ThingDef tDef)`: Calculates the appropriate scale factor for drawing a `ThingDef`'s UI icon.
    *   `ErrorDialog(string message)`: Opens a standard `Dialog_MessageBox` with the given error message.

## 5. `Verse.Listing_Standard` (Vertical Layout Helper)

*   **Purpose:** Simplifies creating vertical lists of standard UI elements with consistent spacing. Automatically manages vertical position (`curY`) and provides methods for common controls.

*   **Core Workflow:**
    1.  `var listing = new Listing_Standard();` (Optionally pass `GameFont` or bounding `Rect` + scroll getter).
    2.  `listing.Begin(Rect totalRect);` (Define the listing area).
    3.  Call widget methods (see below).
    4.  `listing.End();`

*   **Key Methods (Cheat Sheet):**
    *   **Setup & Teardown:**
        *   `Begin(Rect rect)`: Starts the listing within the specified area. Sets `Text.Font`.
        *   `End()`: Finalizes the listing.
    *   **Spacing & Positioning:**
        *   `Gap(float gapHeight)`: Adds vertical space.
        *   `verticalSpacing`: Property for default gap between elements (default `2f`).
        *   `curY`: Current vertical position (read/write).
        *   `GetRect(float height, float widthPct = 1f)`: Reserves space for a custom element and advances `curY`. Returns the `Rect`.
        *   `NewColumn()`: Moves `curX` to the start of the next column (if width allows), resets `curY`.
        *   `ColumnWidth`: Property for the width of the current column.
    *   **Labels:**
        *   `Label(string / TaggedString label, float maxHeight = -1f, string tooltip = null)`: Standard text label. Handles optional scrolling if `maxHeight` is exceeded.
        *   `LabelDouble(string left, string right, string tip = null)`: Two labels side-by-side.
        *   `SubLabel(string label, float widthPct)`: Indented label with smaller font and gray color.
    *   **Buttons:**
        *   `ButtonText(string label, string highlightTag = null, float widthPct = 1f)` (`bool`): Standard text button.
        *   `ButtonTextLabeled(string label, string btnLabel, ...)` (`bool`): Label on the left, button on the right.
        *   `ButtonImage(Texture2D tex, float width, float height)` (`bool`): Image button.
        *   `RadioButton(string label, bool active, ...)` (`bool`): Labeled radio button. Returns `true` if clicked (state managed externally).
    *   **Checkboxes:**
        *   `CheckboxLabeled(string label, ref bool checkOn, string tooltip = null, ...)`: Standard checkbox with label.
        *   `CheckboxLabeledSelectable(string label, ref bool selected, ref bool checkOn)` (`bool`): List item that is both selectable and has a checkbox. Returns `true` on selection change.
    *   **Text Input:**
        *   `TextEntry(string text, int lineCount = 1)` (`string`): Simple text field or text area.
        *   `TextEntryLabeled(string label, string text, int lineCount = 1)` (`string`): Labeled text field/area.
        *   `TextFieldNumeric<T>(ref T val, ref string buffer, ...)`: Input field for numeric types.
        *   `TextFieldNumericLabeled<T>(string label, ref T val, ref string buffer, ...)`: Labeled numeric input field.
    *   **Sliders & Ranges:**
        *   `Slider(float val, float min, float max)` (`float`): Basic horizontal slider.
        *   `SliderLabeled(string label, float val, ...)` (`float`): Labeled horizontal slider.
        *   `IntRange(ref IntRange range, int min, int max)`: Control for selecting min/max integers.
    *   **Adjusters:**
        *   `IntAdjuster(ref int val, int countChange, int min = 0)`: +/- buttons to adjust an integer value.
        *   `IntEntry(ref int val, ref string editBuffer, ...)`: Numeric input with +/- buttons and direct entry.
    *   **Sections:**
        *   `BeginSection(float height, ...)` (`Listing_Standard`): Draws a framed section background and returns a new listing for the inner area.
        *   `EndSection(Listing_Standard listing)`: Ends the section listing.
    *   **Misc:**
        *   `None()`: Displays a centered "(none)" label.
        *   `SelectableDef(string name, bool selected, Action deleteCallback)` (`bool`): Helper for lists of Defs with selection and delete button.
    *   **Debug Tools (Specific use):**
        *   `LabelCheckboxDebug(...)`
        *   `ButtonDebug(...)`
        *   `ButtonDebugPinnable(...)`
        *   `CheckboxPinnable(...)`

*   **Optional Bounding/Scrolling:**
    *   Can be constructed with a `boundingRect` and `boundingScrollPositionGetter` to enable automatic culling of elements outside the visible scroll area (`BoundingRectCached`).

## 6. `Verse.WidgetRow` (Horizontal Layout Helper)

*   **Purpose:** Arranges multiple small UI elements (usually icons or small buttons) horizontally in a single row, advancing the drawing position automatically. Useful for toolbars, gizmos, or within rows created by `Listing_Standard`.

*   **Core Workflow:**
    1.  `var row = new WidgetRow(startX, startY, growDirection = UIDirection.RightThenUp, maxWidth = 99999f, gap = 4f);` (Initialize position, direction, max width, and spacing).
    2.  Call element methods (`row.Icon(...)`, `row.ButtonIcon(...)`, `row.Label(...)`, etc.) repeatedly.
    3.  Each call draws the element and updates `curX` (and potentially `curY` if `maxWidth` is exceeded).

*   **Key Methods & Properties:**
    *   **Initialization:**
        *   `WidgetRow(float x, float y, ...)`: Constructor.
        *   `Init(float x, float y, ...)`: Re-initialize an existing instance.
        *   `growDirection` (`UIDirection`): Controls if the row grows Right/Left and wraps Up/Down.
        *   `maxWidth` (`float`): How wide the row can be before wrapping to the next line.
        *   `gap` (`float`): Default horizontal space between elements.
    *   **Positioning:**
        *   `Gap(float width)`: Adds horizontal space.
        *   `FinalX`, `FinalY`: Read-only properties for the final cursor position after adding elements.
    *   **Icons & Buttons:**
        *   `Icon(Texture tex, string tooltip = null)` (`Rect`): Draws a standard 24x24 icon.
        *   `DefIcon(ThingDef def, string tooltip = null)` (`Rect`): Draws a Def's icon.
        *   `ButtonIcon(Texture2D tex, string tooltip = null, ...)` (`bool`): Draws a 24x24 icon button. Returns `true` if clicked.
        *   `ButtonIconRect(float overrideSize = -1f)` (`Rect`): Calculates and returns the `Rect` for a `ButtonIcon` *without* drawing it or advancing the row position.
        *   `ButtonIconWithBG(Texture2D texture, float width = -1f, ...)` (`bool`): Draws an icon button with a standard background, slightly larger than `ButtonIcon`.
        *   `ToggleableIcon(ref bool toggleable, Texture2D tex, ...)`: Draws an icon button that acts as a toggle, showing a checkmark state.
        *   `ButtonText(string label, string tooltip = null, ...)` (`bool`): Draws a text button, sized to fit the label plus padding.
        *   `ButtonRect(string label, float? fixedWidth = null)` (`Rect`): Calculates and returns the `Rect` for a `ButtonText` *without* drawing it or advancing the row position.
    *   **Labels:**
        *   `Label(string text, float width = -1f, ...)` (`Rect`): Draws text. If `width` is -1, sizes to fit text.
        *   `LabelEllipses(string text, float width, ...)` (`Rect`): Draws text, adding "..." if it exceeds the specified `width`.
    *   **Other:**
        *   `FillableBar(float width, float height, float fillPct, ...)` (`Rect`): Draws a horizontal fillable bar.
        *   `TextFieldNumeric<T>(ref int val, ref string buffer, ...)` (`Rect`): Draws a small numeric input field.

## 7. `Verse.Text` (Drawing State - Text)

*   **Purpose:** A static class responsible for managing the global state of text rendering within the IMGUI system. It controls font size, alignment, and word wrapping, provides access to pre-configured `GUIStyle` objects, and offers utilities for calculating text dimensions.

*   **Key Static Properties (State Control):**
    *   `Font` (`GameFont` enum: Tiny, Small, Medium): Gets or sets the current font size. Setting to `Tiny` might automatically switch to `Small` if `TinyFontSupported` is false.
    *   `Anchor` (`TextAnchor` enum): Gets or sets the current text alignment (e.g., `UpperLeft`, `MiddleCenter`, `LowerRight`).
    *   `WordWrap` (`bool`): Gets or sets whether text should wrap within its container `Rect`.

*   **Usage Pattern (State Management):**
    *   Because these properties control global state, it's crucial to save the current state before changing it, draw the text element(s), and then restore the original state. This prevents unintended style changes for subsequent UI elements.
    *   Example:
        ```csharp
        GameFont originalFont = Text.Font;
        TextAnchor originalAnchor = Text.Anchor;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(myRect, "Centered Medium Text");
        Text.Font = originalFont;
        Text.Anchor = originalAnchor;
        ```

*   **Pre-configured `GUIStyle` Access:**
    *   `CurFontStyle` (`GUIStyle`): Returns a `GUIStyle` for labels, configured with the current `Font`, `Anchor`, and `WordWrap` settings.
    *   `CurTextFieldStyle` (`GUIStyle`): Returns a `GUIStyle` for single-line text fields, configured for the current `Font`.
    *   `CurTextAreaStyle` (`GUIStyle`): Returns a `GUIStyle` for multi-line text areas, configured for the current `Font`.
    *   `CurTextAreaReadOnlyStyle` (`GUIStyle`): Similar to `CurTextAreaStyle` but without background textures, suitable for read-only display.

*   **Text Size Calculation:**
    *   `CalcHeight(string text, float width)` (`float`): Calculates the required height for the given `text` when constrained to the specified `width`, considering the current `Font` and `WordWrap` state. **Important:** Strips rich text tags (`<color=..>`, `<b>`, etc.) before calculating.
    *   `CalcSize(string text)` (`Vector2`): Calculates the required width and height for the given `text` *without* word wrapping, based on the current `Font`. **Important:** Strips rich text tags before calculating.
    *   `ClampTextWithEllipsis(Rect rect, string text)` (`string`): Attempts to fit the text within the rect width, adding "..." if necessary.

*   **Constants & Helpers:**
    *   `LineHeight` (`float`): Read-only. Returns the standard line height for the current `Font`.
    *   `LineHeightOf(GameFont font)` (`float`): Returns the line height for a specific `GameFont`.
    *   `SpaceBetweenLines` (`float`): Read-only. Returns the extra vertical space between lines for the current `Font`.
    *   `TinyFontSupported` (`bool`): Read-only. Indicates if the `Tiny` font can be reliably used (depends on language, preferences, and platform).
    *   `SmallFontHeight` (`const float`): The fixed height (22f) associated with `GameFont.Small`.

*   **Initialization & Reset:**
    *   `StartOfOnGUI()`: Called automatically at the beginning of each GUI frame. Resets `Font` to `Small`, `Anchor` to `UpperLeft`, and `WordWrap` to `true`. Logs errors if the state wasn't properly reset by the end of the previous frame.

## 8. `Verse.KeyBindingDef` (Handling Standard Key Commands)

*   **Purpose:** Represents abstract keybindings defined by the game or mods (e.g., "Cancel", "Accept", "Toggle Pause"). Allows reacting to user-configured keys.
*   **Checking Input:**
    *   `KeyBindingDefOf.Cancel.KeyDownEvent`: Returns `true` if the key(s) bound to the "Cancel" action were just pressed *during an `EventType.KeyDown` event*.
    *   `KeyBindingDefOf.Accept.KeyDownEvent`: Similar for the "Accept" action.
*   **Usage:** Typically used within `Verse.Window` overrides like `OnCancelKeyPressed()` / `OnAcceptKeyPressed()` or directly in `DoWindowContents` to check for specific actions.

## 9. `UnityEngine.Rect` (Positioning & Size)

*   **Purpose:** Fundamental struct for defining position (`x`, `y`) and size (`width`, `height`) of UI elements.
*   **Usage:** Used extensively as parameters for `Widgets` methods and layout helpers. Manual calculation and division of `Rect`s is common.

## 10. `UnityEngine.GUI` (Drawing State - Color)

*   **Purpose:** Controls global drawing state, primarily color.
*   **Key Static Properties:**
    *   `GUI.color`: Tint for most elements and textures.
    *   `GUI.contentColor`: Color for text elements drawn via `GUI` directly (less common, `Text` state is preferred for labels).
    *   `GUI.backgroundColor`: Background for some `GUI` elements.
*   **Usage Pattern:**
    1.  Save current color: `Color originalColor = GUI.color;`
    2.  Set desired color: `GUI.color = Color.red;`
    3.  Draw element(s): `Widgets.DrawBox(...)`
    4.  Restore original color: `GUI.color = originalColor;`

## 11. `UnityEngine.Event` (Input Handling)

*   **Purpose:** Provides information about the current user input event within the `OnGUI` cycle. Crucial for making UI interactive.
*   **Access:** `Event.current`.
*   **Key Properties for Checks:**
    *   `Event.current.type`: The type of event (`EventType.MouseDown`, `MouseUp`, `KeyDown`, `KeyUp`, `ScrollWheel`, `Repaint`, `Layout`).
    *   `Event.current.mousePosition`: Cursor position relative to the current GUI group (usually the window).
    *   `Event.current.button`: Mouse button index (0=left, 1=right, 2=middle).
    *   `Event.current.keyCode`: The key that was pressed/released.
    *   `Event.current.modifiers`: Check for Shift, Ctrl, Alt (`EventModifiers`).
*   **Consuming Input:** `Event.current.Use()`. Call this after successfully handling an event to prevent other elements from processing it. `Widgets` often call this internally.

