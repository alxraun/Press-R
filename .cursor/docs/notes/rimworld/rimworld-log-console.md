# RimWorld Log Console UI Research

## 1. Overview

The RimWorld debug log console provides a window to view messages, warnings, and errors generated during gameplay or development. It's implemented using the standard IMGUI system.

## 2. Key Classes

*   **`Verse.Log` (Static Class):**
    *   **Purpose:** Central point for logging messages (`Log.Message`, `Log.Warning`, `Log.Error`). Also provides methods for clearing (`Log.Clear()`) and attempting to open the log window (`Log.TryOpenLogWindow()`).
    *   **Storage:** Uses an internal `LogMessageQueue` to store `LogMessage` objects.
    *   **Limits:** Implements a message limit (`StopLoggingAtMessageCount`) to prevent spam.
    *   **Auto-Open:** Can be configured (via `Prefs` or `DebugSettings`) to automatically open the log window upon warnings or errors.
    *   **Threading:** Handles messages logged from other threads via `Notify_MessageReceivedThreadedInternal`.

*   **`Verse.LogMessage`:**
    *   **Purpose:** Represents a single log entry.
    *   **Data:** Contains the message text (`text`), type (`LogMessageType`), stack trace (`StackTrace`), and repeat count (`repeats`).

*   **`Verse.LogMessageQueue`:**
    *   **Purpose:** Internal queue holding the actual `LogMessage` objects displayed in the window.
    *   **Notifications:** Notifies `EditWindow_Log` when a message is dequeued (due to queue limits) so the selection can be cleared if necessary.

*   **`LudeonTK.EditWindow_Log` (Inherits `Verse.EditWindow`):**
    *   **Purpose:** The actual UI window for displaying the log messages.
    *   **Base:** Inherits standard window features (`InitialSize`, `optionalTitle`, closing behavior) from `EditWindow`.
    *   **Drawing (`DoWindowContents`):**
        *   Draws top buttons (Clear, Trace size adjustment, Auto-open toggle, Copy, Pause on Error toggle).
        *   Uses `DevGUI.BeginScrollView` to display the list of messages from `Log.Messages`.
        *   Draws each message row, alternating background color (`AltMessageTex`). Displays repeat count and message text.
        *   Handles message selection: Clicking a message sets `selectedMessage`.
        *   Draws a resizable details pane at the bottom showing the `selectedMessage.text` and `selectedMessage.StackTrace` in a separate scrollable text area (`DevGUI.TextAreaScrollable`).
    *   **Interaction:** Allows clicking messages, dragging the border to resize the details pane.
    *   **State:** Manages scroll positions (`messagesScrollPosition`, `detailsScrollPosition`), selected message (`selectedMessage`), details pane height (`detailsPaneHeight`), and auto-open state (`canAutoOpen`).

*   **`Verse.DebugWindowsOpener`:**
    *   **Purpose:** Part of the core debug UI shown when Dev Mode is active.
    *   **Action:** Contains the button ("Open log") that creates and adds a new `EditWindow_Log` instance to the `Find.WindowStack`.

*   **`Verse.UIRoot`:**
    *   **Purpose:** Main UI loop handler.
    *   **Action:** Checks `EditWindow_Log.wantsToOpen` flag each frame and adds the window if requested (e.g., by `Log.TryOpenLogWindow`).

## 3. Data Flow & UI Structure

```ASCII
[`Log.Message/Warning/Error()`] --> [`LogMessageQueue.Enqueue()`] --> [`Log.Messages` (IEnumerable)]
         |                                                           |
         |                               [`LudeonTK.EditWindow_Log.DoWindowContents()`]
         |                                         |       |
         |                             [`DevGUI.BeginScrollView`] ... Loops through `Log.Messages`
         |                                         |       |
         +---- Calls `Log.TryOpenLogWindow()` ----> |     [`DevGUI.Label` (Repeat Count)]
                 (on Warn/Error)                   |     [`DevGUI.ButtonInvisible` (Select Row)] --> Sets `selectedMessage`
                                                   |
                               [`DevGUI.TextAreaScrollable` (Details Pane)] <--- Reads `selectedMessage.text` + `StackTrace`

[`DebugWindowsOpener`] --> Creates & Adds --> [`EditWindow_Log`] --> [`Find.WindowStack`]
[`UIRoot`] ----------> Checks `wantsToOpen`, Adds --> [`EditWindow_Log`] --> [`Find.WindowStack`]
```

## 4. UI Elements & Layout (`EditWindow_Log`)

*   **Top Buttons:** Standard `Widgets.ButtonText` (via internal `DoRowButton` helper).
*   **Message List:**
    *   Uses `DevGUI.BeginScrollView` / `EndScrollView` (likely a wrapper around `Widgets.BeginScrollView`).
    *   Manual layout calculation within the scroll view loop:
        *   Calculates height needed for each message (`Text.CalcHeight`).
        *   Draws alternating backgrounds (`GUI.DrawTexture` with `AltMessageTex`).
        *   Draws selected background (`SelectedMessageTex`).
        *   Uses `DevGUI.Label` for repeat count.
        *   Uses `DevGUI.ButtonInvisible` over the message area for selection.
*   **Details Pane:**
    *   Resizable border (handled via mouse events and `Rect` checks).
    *   Uses `DevGUI.TextAreaScrollable` to display the full text and stack trace of the `selectedMessage`.

## 5. Opening the Window

1.  **Manual:** User clicks "Open log" button in the debug menu (`DebugWindowsOpener`).
2.  **Automatic (Conditional):**
    *   `Log.Error()` or `Log.Warning()` calls `Log.TryOpenLogWindow()`.
    *   `Log.TryOpenLogWindow()` sets `EditWindow_Log.wantsToOpen = true`.
    *   `UIRoot.UIRootOnGUI()` detects `wantsToOpen == true` and adds the window.
    *   Controlled by `Prefs.OpenLogOnWarnings` and `Prefs.DevMode` (errors always open in Dev Mode).
    *   Can be disabled within the log window itself (`canAutoOpen` flag).

## 6. Conclusion

The log console is a relatively straightforward IMGUI window built using standard RimWorld UI techniques. It retrieves data from the static `Log` class and displays it in a scrollable list with a details pane. Interaction is handled via standard button clicks and manual `Rect` checks for selection and resizing. 
