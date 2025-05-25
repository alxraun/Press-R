using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Debug.ValueMonitor
{
    public class ValueMonitorWindow : Window
    {
        private static ValueMonitorWindow _instance;

        private Vector2 _snapshotScrollPosition = Vector2.zero;
        private Vector2 _logScrollPosition = Vector2.zero;
        private bool _showLogConsole = false;

        private string _briefMessage;
        private float _briefMessageUntil = -1f;

        private const float ButtonHeight = 28f;
        private const float ElementSpacing = 8f;
        private const float BriefMessageDuration = 2.0f;
        private const float LogConsoleHeight = 150f;
        private const string LogPrefix = "[ValueMonitor] ";

        private static readonly Color RowEvenColor = new Color(0.21f, 0.21f, 0.21f, 0.7f);
        private static readonly Color RowOddColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);
        private static readonly Color RowErrorColor = new Color(0.5f, 0.1f, 0.1f, 0.7f);
        private static readonly Color RowHighlightColor = new Color(0.3f, 0.3f, 0.2f, 0.7f);

        private const float SnapshotKeyColumnWidth = 0.4f;
        private const float RowPadding = 4f;
        private const float MinRowHeight = 24f;

        private const float TooltipDelay = 0.4f;
        private const float MaxTooltipWidth = 400f;

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        public static bool IsWindowOpen => _instance != null;

        public ValueMonitorWindow()
        {
            draggable = true;
            resizeable = true;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            forcePause = false;
            preventCameraMotion = false;
        }

        public static void ToggleWindow()
        {
            if (_instance != null)
            {
                CloseWindow();
            }
            else
            {
                ShowWindow();
            }
        }

        public static void ShowWindow()
        {
            if (_instance == null)
            {
                _instance = new ValueMonitorWindow();
                Find.WindowStack.Add(_instance);
            }
            else
            {
                Find.WindowStack.TryRemove(_instance.GetType(), false);
                Find.WindowStack.Add(_instance);
            }
        }

        public static void CloseWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            ValueMonitorCore.StopRecording();
            _instance = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                Text.Font = GameFont.Small;
                float currentY = 0f;
                float availableWidth = inRect.width;

                Listing_Standard headerCalcListing = new Listing_Standard();
                Rect headerCalcRect = new Rect(0, 0, availableWidth, 9999f);
                headerCalcListing.Begin(headerCalcRect);

                headerCalcListing.Gap(ElementSpacing * 0.5f);
                headerCalcListing.GetRect(ButtonHeight);
                if (!string.IsNullOrEmpty(ValueMonitorCore.ConfigLoadingError))
                {
                    headerCalcListing.Gap(ElementSpacing * 0.5f);

                    headerCalcListing.Label(ValueMonitorCore.ConfigLoadingError);
                }
                headerCalcListing.Gap(ElementSpacing);
                float headerHeight = headerCalcListing.CurHeight;
                headerCalcListing.End();

                Rect headerRect = new Rect(
                    inRect.x,
                    inRect.y + currentY,
                    availableWidth,
                    headerHeight
                );
                Listing_Standard headerListing = new Listing_Standard();
                headerListing.Begin(headerRect);
                DrawHelpButton(headerListing);
                headerListing.Gap(ElementSpacing * 0.5f);
                DrawConfigSelector(headerListing);
                if (!string.IsNullOrEmpty(ValueMonitorCore.ConfigLoadingError))
                {
                    headerListing.Gap(ElementSpacing * 0.5f);
                    GUI.color = Color.red;
                    headerListing.Label(ValueMonitorCore.ConfigLoadingError);
                    GUI.color = Color.white;
                }
                headerListing.End();
                currentY += headerRect.height + ElementSpacing;

                float statusLabelHeight = 24f;
                float controlsHeight = ButtonHeight * 2 + ElementSpacing;
                float briefMessageHeight = 24f;
                float toggleHeight = 24f;
                float logAreaHeight = 0f;
                if (_showLogConsole)
                {
                    logAreaHeight = ElementSpacing + ButtonHeight + LogConsoleHeight;
                }

                float totalBottomSectionHeight =
                    statusLabelHeight
                    + controlsHeight
                    + briefMessageHeight
                    + toggleHeight
                    + logAreaHeight
                    + (ElementSpacing * 4);

                float snapshotHeight = Mathf.Max(
                    150f,
                    inRect.height - currentY - totalBottomSectionHeight
                );
                Rect snapshotRect = new Rect(
                    inRect.x,
                    inRect.y + currentY,
                    availableWidth,
                    snapshotHeight
                );
                DrawSnapshotScrollView(snapshotRect);
                currentY += snapshotRect.height + ElementSpacing;

                Rect statusRect = new Rect(
                    inRect.x,
                    inRect.y + currentY,
                    availableWidth,
                    statusLabelHeight
                );
                Listing_Standard statusListing = new Listing_Standard();
                statusListing.Begin(statusRect);
                DrawStatusLabel(statusListing);
                statusListing.End();
                currentY += statusRect.height + ElementSpacing;

                Rect controlsRect = new Rect(
                    inRect.x,
                    inRect.y + currentY,
                    availableWidth,
                    controlsHeight
                );
                Listing_Standard controlsListing = new Listing_Standard();
                controlsListing.Begin(controlsRect);
                DrawControlButtons(controlsListing);
                controlsListing.End();
                currentY += controlsRect.height + ElementSpacing;

                Rect briefMessageRect = new Rect(
                    inRect.x,
                    inRect.y + currentY,
                    availableWidth,
                    briefMessageHeight
                );

                if (!string.IsNullOrEmpty(_briefMessage) && Time.time < _briefMessageUntil)
                {
                    GUI.color = new Color(1f, 0.9f, 0.2f);
                    Widgets.DrawBox(briefMessageRect);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(briefMessageRect.ContractedBy(4f), _briefMessage);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                }
                else
                {
                    _briefMessage = null;
                }

                currentY += briefMessageRect.height + ElementSpacing;

                Rect toggleRect = new Rect(
                    inRect.x,
                    inRect.y + currentY,
                    availableWidth,
                    toggleHeight
                );
                Widgets.CheckboxLabeled(toggleRect, "Show ValueMonitor Log", ref _showLogConsole);
                currentY += toggleRect.height + ElementSpacing;

                if (_showLogConsole)
                {
                    Rect logAreaRect = new Rect(
                        inRect.x,
                        inRect.y + currentY,
                        availableWidth,
                        logAreaHeight
                    );

                    float logControlsRequiredHeight = ButtonHeight + ElementSpacing;
                    float logScrollViewHeight = Mathf.Max(
                        0f,
                        logAreaRect.height - logControlsRequiredHeight
                    );

                    Rect logControlsRect = new Rect(
                        logAreaRect.x,
                        logAreaRect.y,
                        logAreaRect.width,
                        ButtonHeight
                    );
                    Rect logScrollViewOuterRect = new Rect(
                        logAreaRect.x,
                        logAreaRect.y + logControlsRequiredHeight,
                        logAreaRect.width,
                        logScrollViewHeight
                    );

                    Listing_Standard logControlsListing = new Listing_Standard();
                    logControlsListing.Begin(logControlsRect);

                    float buttonWidth = (logControlsRect.width - ElementSpacing) / 2f;
                    Rect copyButtonRect = new Rect(
                        logControlsRect.x,
                        0f,
                        buttonWidth,
                        ButtonHeight
                    );
                    Rect clearButtonRect = new Rect(
                        copyButtonRect.xMax + ElementSpacing,
                        0f,
                        buttonWidth,
                        ButtonHeight
                    );

                    if (Widgets.ButtonText(copyButtonRect, "Copy Log"))
                    {
                        CopyLogToClipboard();
                    }
                    if (Widgets.ButtonText(clearButtonRect, "Clear Log"))
                    {
                        ValueMonitorLog.ClearLogs();
                    }
                    logControlsListing.End();

                    DrawLogConsoleScrollView(logScrollViewOuterRect, logAreaRect.width);
                }
            }
            catch (Exception ex)
            {
                ValueMonitorLog.Error($"{LogPrefix}Error in ValueMonitorWindow: {ex}");
                CloseWindow();
            }
            finally
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        private void DrawHelpButton(Listing_Standard listing)
        {
            Rect helpRect = new Rect(listing.GetRect(0).width - 24f, 0f, 24f, 24f);
            TooltipHandler.TipRegion(
                helpRect,
                new TipSignal(ValueMonitorTrackedValueInfo.ValuePathSyntax.FormatExamplesForHelp())
            );
            if (Widgets.ButtonImage(helpRect, TexButton.Info))
            {
                Find.WindowStack.Add(
                    new Dialog_MessageBox(
                        ValueMonitorTrackedValueInfo.ValuePathSyntax.FormatExamplesForHelp(),
                        "Close",
                        null,
                        null,
                        null,
                        "Path Syntax Help",
                        true
                    )
                );
            }
        }

        private void DrawConfigSelector(Listing_Standard listing)
        {
            var configs =
                ValueMonitorCore.AvailableConfigs?.ToList() ?? new List<IValueMonitorConfig>();
            string currentConfigName = ValueMonitorCore.CurrentConfig?.Name ?? "Select Config";
            Rect buttonRect = listing.GetRect(ButtonHeight);

            if (Widgets.ButtonText(buttonRect, currentConfigName))
            {
                if (configs.Any())
                {
                    var options = new List<FloatMenuOption>();
                    foreach (var config in configs)
                    {
                        options.Add(
                            new FloatMenuOption(
                                config.Name,
                                () => ValueMonitorCore.LoadConfig(config)
                            )
                        );
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                else
                {
                    Messages.Message(
                        "No monitor configurations found.",
                        MessageTypeDefOf.CautionInput
                    );
                }
            }

            if (!string.IsNullOrEmpty(ValueMonitorCore.ConfigLoadingError))
            {
                listing.Gap(ElementSpacing * 0.5f);
                GUI.color = Color.red;
                listing.Label(ValueMonitorCore.ConfigLoadingError);
                GUI.color = Color.white;
            }
        }

        private void DrawStatusLabel(Listing_Standard listing)
        {
            string statusText = "Status: ";

            Color statusColor = Color.white;

            switch (ValueMonitorCore.CurrentRecordingState)
            {
                case RecordingState.Stopped:
                    statusText += "Stopped";
                    statusColor = Color.gray;
                    break;
                case RecordingState.Starting:
                    statusText += $"Starting in {ValueMonitorCore.GetStartDelayTimer():F1}s...";
                    statusColor = new Color(1f, 0.8f, 0.2f);
                    break;
                case RecordingState.Recording:
                    statusText += "Recording";
                    statusColor = new Color(0.2f, 0.9f, 0.2f);
                    break;
                case RecordingState.Paused:
                    statusText += "Paused";
                    statusColor = new Color(0.9f, 0.4f, 0.2f);
                    break;
                default:
                    statusText += "Unknown";
                    break;
            }

            GUI.color = statusColor;
            listing.Label(statusText);
            GUI.color = Color.white;
        }

        private void DrawSnapshotScrollView(Rect outerRect)
        {
            Rect headerRect = new Rect(outerRect.x, outerRect.y, outerRect.width, 22f);
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "Snapshot Data");
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            Rect contentRect = new Rect(
                outerRect.x,
                outerRect.y + 24f,
                outerRect.width,
                outerRect.height - 24f
            );

            Widgets.DrawBoxSolid(contentRect, new Color(0.15f, 0.15f, 0.15f, 0.4f));
            Widgets.DrawBox(contentRect);

            var lastSnapshot = ValueMonitorCore.LastSnapshot;
            if (lastSnapshot == null || !lastSnapshot.Any())
            {
                Widgets.Label(contentRect.ContractedBy(10f), "No snapshot data available");
                return;
            }

            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, 0f);

            float contentHeight = CalculateSnapshotContentHeight(lastSnapshot, viewRect.width);
            viewRect.height = contentHeight;

            Widgets.BeginScrollView(contentRect, ref _snapshotScrollPosition, viewRect);

            float keyColumnWidth = viewRect.width * SnapshotKeyColumnWidth;
            float valueColumnWidth = viewRect.width - keyColumnWidth;
            float keyPadding = 8f;
            float valuePadding = 4f;

            float curY = 0f;

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Rect infoRect = new Rect(0f, curY, viewRect.width, 22f);
            Widgets.DrawBoxSolid(infoRect, new Color(0.18f, 0.18f, 0.18f, 0.7f));
            Widgets.Label(
                infoRect.ContractedBy(keyPadding, 0),
                $"Total items: {lastSnapshot.Count}"
            );
            curY += 22f;
            GUI.color = Color.white;

            bool isAlt = false;
            foreach (var entry in lastSnapshot)
            {
                string keyStr = entry.Key;
                string valueStr = entry.Value?.ToString() ?? "null";

                float keyHeight = Text.CalcHeight(keyStr, keyColumnWidth - keyPadding * 2);
                float valueHeight = Text.CalcHeight(valueStr, valueColumnWidth - valuePadding * 2);
                float rowHeight = Mathf.Max(
                    MinRowHeight,
                    Mathf.Max(keyHeight, valueHeight) + RowPadding * 2
                );

                Rect rowRect = new Rect(0f, curY, viewRect.width, rowHeight);

                bool isError = IsErrorValue(valueStr);

                if (isError)
                {
                    Widgets.DrawBoxSolid(rowRect, RowErrorColor);
                }
                else
                {
                    if (isAlt)
                        Widgets.DrawBoxSolid(rowRect, RowOddColor);
                    else
                        Widgets.DrawBoxSolid(rowRect, RowEvenColor);
                }

                Rect keyRect = new Rect(
                    keyPadding,
                    curY + RowPadding,
                    keyColumnWidth - keyPadding * 2,
                    rowHeight - RowPadding * 2
                );

                Rect valueRect = new Rect(
                    keyColumnWidth + valuePadding,
                    curY + RowPadding,
                    valueColumnWidth - valuePadding * 2,
                    rowHeight - RowPadding * 2
                );

                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(keyRect, keyStr);

                GUI.color = isError ? new Color(1f, 0.4f, 0.4f) : Color.white;
                Text.WordWrap = true;
                Widgets.Label(valueRect, valueStr);
                Text.WordWrap = false;

                if (Mouse.IsOver(valueRect))
                {
                    TooltipHandler.TipRegion(
                        valueRect,
                        new TipSignal(valueStr, entry.GetHashCode())
                    );
                }

                curY += rowHeight;
                isAlt = !isAlt;
            }

            GUI.color = Color.white;
            Widgets.EndScrollView();

            Text.WordWrap = true;
        }

        private float CalculateSnapshotContentHeight(
            Dictionary<string, object> snapshot,
            float availableWidth
        )
        {
            if (snapshot == null || !snapshot.Any())
                return 0f;

            float keyColumnWidth = availableWidth * SnapshotKeyColumnWidth;
            float valueColumnWidth = availableWidth - keyColumnWidth;
            float totalHeight = 22f;

            foreach (var entry in snapshot)
            {
                string keyStr = entry.Key;
                string valueStr = entry.Value?.ToString() ?? "null";

                float keyHeight = Text.CalcHeight(keyStr, keyColumnWidth - 16f);
                float valueHeight = Text.CalcHeight(valueStr, valueColumnWidth - 8f);
                float rowHeight = Mathf.Max(
                    MinRowHeight,
                    Mathf.Max(keyHeight, valueHeight) + RowPadding * 2
                );

                totalHeight += rowHeight;
            }

            return totalHeight;
        }

        private bool IsErrorValue(string valueString)
        {
            if (string.IsNullOrEmpty(valueString))
                return false;

            return valueString.StartsWith("Error:")
                || valueString.StartsWith("Runtime Error:")
                || valueString.StartsWith("Configuration Error:")
                || valueString.StartsWith("Getter Error:")
                || valueString.StartsWith("Path Error:")
                || valueString.StartsWith("Access Error:")
                || valueString.StartsWith("Compilation Error")
                || valueString.StartsWith("Null value encountered")
                || valueString == "Path cannot be empty"
                || valueString.StartsWith("Invalid path format:")
                || valueString.StartsWith("Cannot find")
                || valueString.StartsWith("Could not find")
                || valueString.StartsWith("Value is not");
        }

        private void DrawControlButtons(Listing_Standard listing)
        {
            bool canControl = ValueMonitorCore.CurrentConfig != null;
            bool historyAvailable = ValueMonitorCore.SnapshotsHistory?.Any() ?? false;

            Rect controlRect = listing.GetRect(ButtonHeight * 2 + ElementSpacing);

            Rect row1 = new Rect(controlRect.x, controlRect.y, controlRect.width, ButtonHeight);
            Rect row2 = new Rect(
                controlRect.x,
                controlRect.y + ButtonHeight + ElementSpacing,
                controlRect.width,
                ButtonHeight
            );

            float buttonWidth = (row1.width - ElementSpacing) / 2f;

            Rect startPauseResumeRect = new Rect(row1.x, row1.y, buttonWidth, ButtonHeight);
            Rect stopRect = new Rect(
                startPauseResumeRect.xMax + ElementSpacing,
                row1.y,
                buttonWidth,
                ButtonHeight
            );

            string startPauseResumeLabel = GetActionButtonLabel();
            Action startPauseResumeAction = GetActionButtonAction();

            Color buttonColor;
            switch (ValueMonitorCore.CurrentRecordingState)
            {
                case RecordingState.Stopped:
                    buttonColor = new Color(0.2f, 0.7f, 0.2f);
                    break;
                case RecordingState.Starting:
                    buttonColor = new Color(0.7f, 0.7f, 0.2f);
                    break;
                case RecordingState.Recording:
                    buttonColor = new Color(0.7f, 0.2f, 0.2f);
                    break;
                case RecordingState.Paused:
                    buttonColor = new Color(0.2f, 0.6f, 0.7f);
                    break;
                default:
                    buttonColor = Color.white;
                    break;
            }

            Color originalColor = GUI.color;
            GUI.color = buttonColor;

            if (Widgets.ButtonText(startPauseResumeRect, startPauseResumeLabel, active: canControl))
            {
                startPauseResumeAction?.Invoke();
            }

            GUI.color = new Color(0.7f, 0.3f, 0.3f);

            bool canStop =
                canControl && ValueMonitorCore.CurrentRecordingState != RecordingState.Stopped;
            if (Widgets.ButtonText(stopRect, "Stop", active: canStop))
            {
                ValueMonitorCore.StopRecording();
            }

            GUI.color = new Color(0.3f, 0.5f, 0.7f);
            bool canCopy = canControl && historyAvailable;
            if (Widgets.ButtonText(row2, "Copy History to Clipboard", active: canCopy))
            {
                CopyHistoryToClipboard();
            }

            GUI.color = originalColor;
        }

        private string GetActionButtonLabel()
        {
            switch (ValueMonitorCore.CurrentRecordingState)
            {
                case RecordingState.Stopped:
                    return "Start Recording";
                case RecordingState.Starting:
                    return "Cancel Start";
                case RecordingState.Recording:
                    return "Pause Recording";
                case RecordingState.Paused:
                    return "Resume Recording";
                default:
                    return "Unknown";
            }
        }

        private Action GetActionButtonAction()
        {
            switch (ValueMonitorCore.CurrentRecordingState)
            {
                case RecordingState.Stopped:
                    return ValueMonitorCore.StartRecording;
                case RecordingState.Starting:
                    return ValueMonitorCore.StopRecording;
                case RecordingState.Recording:
                    return ValueMonitorCore.PauseRecording;
                case RecordingState.Paused:
                    return ValueMonitorCore.ResumeRecording;
                default:
                    return null;
            }
        }

        private void DrawBriefMessage(Listing_Standard listing)
        {
            if (!string.IsNullOrEmpty(_briefMessage) && Time.time < _briefMessageUntil)
            {
                GUI.color = new Color(1f, 0.9f, 0.2f);

                Rect messageRect = listing.GetRect(24f);
                Widgets.DrawBox(messageRect);
                messageRect = messageRect.ContractedBy(4f);

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(messageRect, _briefMessage);
                Text.Anchor = TextAnchor.UpperLeft;

                GUI.color = Color.white;
            }
            else
            {
                _briefMessage = null;
            }
        }

        private void CopyHistoryToClipboard()
        {
            try
            {
                string csvData = ValueMonitorCore.GetHistoryAsCsv();
                GUIUtility.systemCopyBuffer = csvData;
                _briefMessage = "History Copied!";
                _briefMessageUntil = Time.time + BriefMessageDuration;
            }
            catch (Exception ex)
            {
                ValueMonitorLog.Error($"{LogPrefix}Failed to copy history to clipboard: {ex}");
                _briefMessage = "Copy Failed!";
                _briefMessageUntil = Time.time + BriefMessageDuration;
            }
        }

        private void DrawLogConsoleScrollView(Rect logScrollViewOuterRect, float viewWidth)
        {
            Widgets.DrawBoxSolid(logScrollViewOuterRect, new Color(0.12f, 0.12f, 0.12f, 0.7f));
            Widgets.DrawBox(logScrollViewOuterRect);

            var logEntries = ValueMonitorLog.GetLogs().ToList();

            float logContentHeight = CalculateLogContentHeight(logEntries);
            Rect logScrollViewInnerRect = new Rect(0f, 0f, viewWidth - 16f, logContentHeight);

            Widgets.BeginScrollView(
                logScrollViewOuterRect,
                ref _logScrollPosition,
                logScrollViewInnerRect
            );

            float currentY = 0f;
            bool isAlternate = false;

            foreach (var entry in logEntries)
            {
                string logText = entry.ToString();
                float textHeight = Text.CalcHeight(logText, logScrollViewInnerRect.width - 8f);
                textHeight = Mathf.Max(MinRowHeight, textHeight + 2f);

                Rect entryRect = new Rect(0f, currentY, logScrollViewInnerRect.width, textHeight);

                if (isAlternate)
                    Widgets.DrawBoxSolid(entryRect, RowOddColor);
                else
                    Widgets.DrawBoxSolid(entryRect, RowEvenColor);

                Color textColor;
                switch (entry.Level)
                {
                    case ValueMonitorLogLevel.Warning:
                        textColor = new Color(1f, 0.85f, 0.4f);
                        break;
                    case ValueMonitorLogLevel.Error:
                        textColor = new Color(1f, 0.4f, 0.4f);
                        break;
                    default:
                        textColor = new Color(0.85f, 0.85f, 0.85f);
                        break;
                }

                GUI.color = textColor;
                Widgets.Label(entryRect.ContractedBy(4f, 0f), logText);

                currentY += textHeight;
                isAlternate = !isAlternate;
            }

            GUI.color = Color.white;
            Widgets.EndScrollView();
        }

        private float CalculateLogContentHeight(List<ValueMonitorLogEntry> logEntries)
        {
            if (!logEntries.Any())
                return Text.LineHeight;

            float totalHeight = 0f;
            float viewWidth = InitialSize.x - 36f;

            foreach (var entry in logEntries)
            {
                float textHeight = Text.CalcHeight(entry.ToString(), viewWidth);
                textHeight = Mathf.Max(MinRowHeight, textHeight + 2f);
                totalHeight += textHeight;
            }

            return totalHeight;
        }

        private void CopyLogToClipboard()
        {
            try
            {
                string logData = ValueMonitorLog.GetLogsAsString();
                GUIUtility.systemCopyBuffer = logData;
                _briefMessage = "Log Copied!";
                _briefMessageUntil = Time.time + BriefMessageDuration;
            }
            catch (Exception ex)
            {
                ValueMonitorLog.Error($"{LogPrefix}Failed to copy log to clipboard: {ex}");
                _briefMessage = "Log Copy Failed!";
                _briefMessageUntil = Time.time + BriefMessageDuration;
            }
        }
    }

    /*
    public static class ValueMonitorCore
    {
        public static float GetStartDelayTimer() => _startDelayTimer;
    }
    */
}
