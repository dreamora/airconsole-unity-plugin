#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using NDream.AirConsole.Editor;

namespace NDream.AirConsole.Editor {
    public class SettingWindow : EditorWindow {
        private GUIStyle styleBlack = new();
        private bool groupEnabled = false;
        private static Texture2D bg;
        private static Texture logo;
        private static Texture logoSmall;
        private static GUIContent titleInfo;

        // UI Polish: Scroll position for responsive layout
        private static Vector2 _scrollPosition = Vector2.zero;

        // UI Polish: Cached GUIStyles for consistent branding
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _smallButtonStyle;
        private GUIStyle _helpTextStyle;
        private GUIStyle _sectionHeaderStyle;
        private bool _stylesInitialized = false;

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

            // UI Polish: Set minimum window size for responsive layout
            minSize = new Vector2(400, 300);

            // UI Polish: Reset style initialization flag so styles are recreated in OnGUI
            _stylesInitialized = false;
        }

        /// <summary>
        /// UI Polish: Initialize custom styles for consistent branding
        /// Must be called from within OnGUI() context
        /// </summary>
        private void InitializeCustomStyles() {
            if (_stylesInitialized) return;

            try {
                // Primary button style for main actions
                _primaryButtonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 28,
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(8, 8, 4, 4)
                };

                // Small button style for tertiary actions (Reset)
                _smallButtonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal,
                    fixedHeight = 28,
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(6, 6, 4, 4)
                };

                // Help text style for tooltips and descriptions
                _helpTextStyle = new GUIStyle(EditorStyles.miniLabel) {
                    wordWrap = true,
                    fontSize = 10,
                    normal = { textColor = Color.gray }
                };

                // Section header style consistent with AirConsole branding
                _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) {
                    fontSize = 13,
                    margin = new RectOffset(0, 0, 10, 5)
                };

                _stylesInitialized = true;
            } catch (System.ArgumentException) {
                // GUI functions not available yet, styles will be initialized on next OnGUI call
                _stylesInitialized = false;
            }
        }

        [MenuItem("Window/AirConsole/Settings")]
        private static void Init() {
            OpenSettingsWindow();
        }

        /// <summary>
        /// Opens the AirConsole Settings window, or focuses it if already open
        /// </summary>
        public static void OpenSettingsWindow() {
            SettingWindow window = (SettingWindow)GetWindow(typeof(SettingWindow));
            window.titleContent = titleInfo;
            window.Show();
            window.Focus();
        }

        /// <summary>
        /// Draws a minimal update notification banner that directs users to the UpdateCheckerWindow
        /// </summary>
        private void DrawUpdateNotificationBanner() {
            var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;

            // Only show banner if update is available
            if (!isUpdateAvailable) {
                return;
            }

            EditorGUILayout.Space(5);

            // Simple update available notification
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.3f); // Light green background

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔄 Update Available", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Button to open UpdateCheckerWindow
            if (GUILayout.Button("Open Update Checker", GUILayout.Width(150))) {
                UpdateCheckerWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("A new version of the AirConsole plugin is available. Use the Update Checker for details.",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }







        /// <summary>
        /// Formats a TimeSpan into a human-readable string
        /// </summary>
        /// <param name="timeSpan">The TimeSpan to format</param>
        /// <returns>Formatted string</returns>
        private string FormatTimeSpan(TimeSpan timeSpan) {
            if (timeSpan.TotalDays >= 1) {
                return $"{(int)timeSpan.TotalDays} day(s)";
            } else if (timeSpan.TotalHours >= 1) {
                return $"{(int)timeSpan.TotalHours} hour(s)";
            } else if (timeSpan.TotalMinutes >= 1) {
                return $"{(int)timeSpan.TotalMinutes} minute(s)";
            } else {
                return "less than a minute";
            }
        }

        private void OnGUI() {
            // UI Polish: Initialize styles if needed
            InitializeCustomStyles();

            // UI Polish: Responsive layout with scrolling for smaller windows
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // show logo & version
            EditorGUILayout.BeginHorizontal(styleBlack, GUILayout.Height(30));
            GUILayout.Label(logo, GUILayout.Width(128), GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + Settings.VERSION, styleBlack);
            EditorGUILayout.EndHorizontal();

            // Update notification banner
            DrawUpdateNotificationBanner();

            // UI Polish: Main settings header with proper spacing
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("AirConsole Settings", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            EditorGUILayout.Space(10);

            // UI Polish: Connection Settings section with proper container structure
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Connection Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // UI Polish: Port settings with tooltips and consistent layout
            EditorGUILayout.BeginHorizontal();
            var webSocketContent = new GUIContent("Websocket Port", "Port used for WebSocket communication with AirConsole");
            Settings.webSocketPort = EditorGUILayout.IntField(webSocketContent, Settings.webSocketPort, GUILayout.Width(200));
            EditorPrefs.SetInt("webSocketPort", Settings.webSocketPort);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var webServerContent = new GUIContent("Webserver Port", "Port used for the local web server");
            Settings.webServerPort = EditorGUILayout.IntField(webServerContent, Settings.webServerPort, GUILayout.Width(200));
            EditorPrefs.SetInt("webServerPort", Settings.webServerPort);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // UI Polish: Server status with better formatting and proper row layout
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Webserver Status:", GUILayout.Width(120));
            var isRunning = Extentions.webserver.IsRunning();
            var statusColor = isRunning ? Color.green : Color.red;
            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(isRunning ? "✓ Running" : "✗ Stopped", GUILayout.Width(80));
            GUI.color = originalColor;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // UI Polish: Debug Settings section with proper container structure
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            groupEnabled = EditorGUILayout.BeginToggleGroup("Enable Debug Logging", groupEnabled);

            var infoContent = new GUIContent("Info", "Show informational debug messages");
            Settings.debug.info = EditorGUILayout.Toggle(infoContent, Settings.debug.info);
            EditorPrefs.SetBool("debugInfo", Settings.debug.info);

            var warningContent = new GUIContent("Warning", "Show warning debug messages");
            Settings.debug.warning = EditorGUILayout.Toggle(warningContent, Settings.debug.warning);
            EditorPrefs.SetBool("debugWarning", Settings.debug.warning);

            var errorContent = new GUIContent("Error", "Show error debug messages");
            Settings.debug.error = EditorGUILayout.Toggle(errorContent, Settings.debug.error);
            EditorPrefs.SetBool("debugError", Settings.debug.error);

            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndVertical();

            // UI Polish: Footer with reset button and proper spacing
            EditorGUILayout.Space(15);

            // UI Polish: Footer container with proper button layout
            EditorGUILayout.BeginVertical(styleBlack);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // UI Polish: Reset button with consistent styling and proper spacing
            var resetContent = new GUIContent("Reset Settings", "Reset all AirConsole settings to default values");
            if (GUILayout.Button(resetContent, _smallButtonStyle ?? GUI.skin.button, GUILayout.Width(110))) {
                ShowResetConfirmationDialog();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // UI Polish: End scroll view
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// UI Polish: Shows confirmation dialog for resetting settings
        /// </summary>
        private void ShowResetConfirmationDialog() {
            var title = "Reset All Settings";
            var message = "This will reset all AirConsole settings to their default values.\n\n" +
                         "This includes:\n" +
                         "• Update checking preferences\n" +
                         "• Port configurations\n" +
                         "• Debug settings\n\n" +
                         "This action cannot be undone. Continue?";

            if (EditorUtility.DisplayDialog(title, message, "Reset Settings", "Cancel")) {
                Extentions.ResetDefaultValues();

                // Also reset update settings
                var updateSettings = UpdateSettings.Instance;
                updateSettings.AutomaticCheckEnabled = true;
                updateSettings.DismissedVersion = "";

                // Restart update checking with new settings
                UpdateChecker.RestartAutomaticChecking();

                // No success dialog needed - user can see the settings have been reset in the UI
                // Force UI refresh to show the reset values
                Repaint();
            }
        }
    }
}
#endif