#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace NDream.AirConsole.Editor {
    public class SettingWindow : EditorWindow {
        private GUIStyle styleBlack = new();
        private bool groupEnabled = false;
        private static Texture2D bg;
        private static Texture logo;
        private static Texture logoSmall;
        private static GUIContent titleInfo;

        public void OnEnable() {
            // get images
            bg = (Texture2D)Resources.Load("AirConsoleBg");
            logo = (Texture)Resources.Load("AirConsoleLogoText");
            logoSmall = (Texture)Resources.Load("AirConsoleLogoSmall");
            titleInfo = new GUIContent("AirConsole", logoSmall, "AirConsole Settings");

            // setup style for airconsole logo
            styleBlack.normal.background = bg;
            styleBlack.normal.textColor = Color.white;
            styleBlack.margin.top = 5;
            styleBlack.padding.right = 5;
        }

        [MenuItem("Window/AirConsole/Settings")]
        private static void Init() {
            SettingWindow window = (SettingWindow)GetWindow(typeof(SettingWindow));
            window.titleContent = titleInfo;
            window.Show();
        }

        /// <summary>
        /// Draws the update notification banner if an update is available or check is in progress
        /// </summary>
        private void DrawUpdateNotificationBanner() {
            var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
            var isCheckInProgress = UpdateChecker.IsCheckInProgress;
            var canCheckNow = UpdateChecker.CanCheckNow();

            // Only show banner if there's something to display
            if (!isUpdateAvailable && !isCheckInProgress && canCheckNow) {
                return;
            }

            EditorGUILayout.Space(5);

            // Update available banner
            if (isUpdateAvailable) {
                DrawUpdateAvailableBanner();
            }
            // Check in progress banner
            else if (isCheckInProgress) {
                DrawCheckInProgressBanner();
            }
            // Rate limiting status
            else if (!canCheckNow) {
                DrawRateLimitingBanner();
            }

            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draws the update available notification banner
        /// </summary>
        private void DrawUpdateAvailableBanner() {
            var currentVersion = UpdateChecker.CurrentVersion;
            var latestVersion = UpdateChecker.LatestVersion;
            var lastCheckTime = UpdateChecker.LastCheckTime;

            // Create a colored background for the update notification
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.3f); // Light green background

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            EditorGUILayout.LabelField("🔄 Update Available", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Current: v{currentVersion}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Available: v{latestVersion}", GUILayout.Width(120));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("View Release", GUILayout.Width(100))) {
                UpdateChecker.OpenReleasePage();
            }

            if (GUILayout.Button("Dismiss", GUILayout.Width(70))) {
                UpdateChecker.DismissCurrentUpdate();
            }
            EditorGUILayout.EndHorizontal();

            // Show last check time
            if (lastCheckTime != DateTime.MinValue) {
                var timeSinceCheck = DateTime.Now - lastCheckTime;
                string timeText;
                if (timeSinceCheck.TotalDays >= 1) {
                    timeText = $"{(int)timeSinceCheck.TotalDays} day(s) ago";
                } else if (timeSinceCheck.TotalHours >= 1) {
                    timeText = $"{(int)timeSinceCheck.TotalHours} hour(s) ago";
                } else {
                    timeText = $"{(int)timeSinceCheck.TotalMinutes} minute(s) ago";
                }
                EditorGUILayout.LabelField($"Last checked: {timeText}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the check in progress banner
        /// </summary>
        private void DrawCheckInProgressBanner() {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 0.3f); // Light blue background

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔄 Checking for updates...", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Simple loading indicator using rotating characters
            var loadingChars = new char[] { '|', '/', '-', '\\' };
            var loadingIndex = (int)(EditorApplication.timeSinceStartup * 4) % loadingChars.Length;
            EditorGUILayout.LabelField(loadingChars[loadingIndex].ToString(), GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Repaint to animate the loading indicator
            Repaint();
        }

        /// <summary>
        /// Draws the rate limiting status banner
        /// </summary>
        private void DrawRateLimitingBanner() {
            var timeUntilNext = UpdateChecker.TimeUntilNextCheck();

            if (timeUntilNext == TimeSpan.Zero || timeUntilNext == TimeSpan.MaxValue) {
                return;
            }

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.3f); // Light orange background

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            string timeText;
            if (timeUntilNext.TotalDays >= 1) {
                timeText = $"{(int)timeUntilNext.TotalDays} day(s)";
            } else if (timeUntilNext.TotalHours >= 1) {
                timeText = $"{(int)timeUntilNext.TotalHours} hour(s)";
            } else {
                timeText = $"{(int)timeUntilNext.TotalMinutes} minute(s)";
            }

            EditorGUILayout.LabelField($"⏱️ Next update check available in {timeText}", EditorStyles.miniLabel);

            var lastCheckTime = UpdateChecker.LastCheckTime;
            if (lastCheckTime != DateTime.MinValue) {
                var timeSinceCheck = DateTime.Now - lastCheckTime;
                string lastCheckText;
                if (timeSinceCheck.TotalDays >= 1) {
                    lastCheckText = $"{(int)timeSinceCheck.TotalDays} day(s) ago";
                } else if (timeSinceCheck.TotalHours >= 1) {
                    lastCheckText = $"{(int)timeSinceCheck.TotalHours} hour(s) ago";
                } else {
                    lastCheckText = $"{(int)timeSinceCheck.TotalMinutes} minute(s) ago";
                }
                EditorGUILayout.LabelField($"Last checked: {lastCheckText}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void OnGUI() {
            // show logo & version
            EditorGUILayout.BeginHorizontal(styleBlack, GUILayout.Height(30));
            GUILayout.Label(logo, GUILayout.Width(128), GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + Settings.VERSION, styleBlack);
            EditorGUILayout.EndHorizontal();

            // Update notification banner
            DrawUpdateNotificationBanner();

            GUILayout.Label("AirConsole Settings", EditorStyles.boldLabel);

            // Manual update check button for testing
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check for Updates", GUILayout.Width(150))) {
                UpdateChecker.CheckForUpdatesAsync(force: true);
            }
            if (GUILayout.Button("Clear Dismissed", GUILayout.Width(120))) {
                UpdateChecker.ClearDismissedVersion();
            }
            EditorGUILayout.EndHorizontal();

            // Debug information
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Info:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Update Available: {UpdateChecker.IsUpdateAvailable}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Check In Progress: {UpdateChecker.IsCheckInProgress}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Can Check Now: {UpdateChecker.CanCheckNow()}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Current Version: {UpdateChecker.CurrentVersion}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Latest Version: {UpdateChecker.LatestVersion}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            Settings.webSocketPort = EditorGUILayout.IntField("Websocket Port", Settings.webSocketPort, GUILayout.MaxWidth(200));
            EditorPrefs.SetInt("webSocketPort", Settings.webSocketPort);

            Settings.webServerPort = EditorGUILayout.IntField("Webserver Port", Settings.webServerPort, GUILayout.MaxWidth(200));
            EditorPrefs.SetInt("webServerPort", Settings.webServerPort);

            EditorGUILayout.LabelField("Webserver is running", Extentions.webserver.IsRunning().ToString());

            groupEnabled = EditorGUILayout.BeginToggleGroup("Debug Settings", groupEnabled);

            Settings.debug.info = EditorGUILayout.Toggle("Info", Settings.debug.info);
            EditorPrefs.SetBool("debugInfo", Settings.debug.info);

            Settings.debug.warning = EditorGUILayout.Toggle("Warning", Settings.debug.warning);
            EditorPrefs.SetBool("debugWarning", Settings.debug.warning);

            Settings.debug.error = EditorGUILayout.Toggle("Error", Settings.debug.error);
            EditorPrefs.SetBool("debugError", Settings.debug.error);

            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.BeginHorizontal(styleBlack);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset Settings", GUILayout.MaxWidth(110))) {
                Extentions.ResetDefaultValues();
            }

            GUILayout.EndHorizontal();
        }
    }
}
#endif
