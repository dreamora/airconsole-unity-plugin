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

        // UI Polish: Animation and styling variables
        private static float _updateBannerAlpha = 0f;
        private static float _targetBannerAlpha = 0f;
        private static double _lastRepaintTime = 0;
        private static bool _showUpdateSettings = true;
        private static Vector2 _scrollPosition = Vector2.zero;

        // UI Polish: Cached GUIStyles for consistent branding
        private GUIStyle _updateBannerStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
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
                // Update banner style with AirConsole branding colors
                _updateBannerStyle = new GUIStyle(EditorStyles.helpBox) {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(5, 5, 5, 5)
                };

                // Primary button style for main actions (Update Now, Check Now)
                _primaryButtonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 28,
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(8, 8, 4, 4)
                };

                // Secondary button style for secondary actions (Release Notes, Show Dismissed)
                _secondaryButtonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal,
                    fixedHeight = 28,
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(6, 6, 4, 4)
                };

                // Small button style for tertiary actions (Dismiss, Reset)
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
        /// Draws the update notification banner if an update is available or check is in progress
        /// UI Polish: Includes smooth transitions and responsive layout
        /// </summary>
        private void DrawUpdateNotificationBanner() {
            var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
            var isCheckInProgress = UpdateChecker.IsCheckInProgress;
            var canCheckNow = UpdateChecker.CanCheckNow();

            // UI Polish: Determine target alpha for smooth transitions
            bool shouldShowBanner = isUpdateAvailable || isCheckInProgress || !canCheckNow;
            _targetBannerAlpha = shouldShowBanner ? 1f : 0f;

            // UI Polish: Animate banner alpha for smooth transitions
            UpdateBannerAnimation();

            // Only draw if banner has some visibility
            if (_updateBannerAlpha <= 0.01f) {
                return;
            }

            EditorGUILayout.Space(5);

            // UI Polish: Apply alpha for smooth transitions
            var originalColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _updateBannerAlpha);

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

            GUI.color = originalColor;
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// UI Polish: Updates banner animation for smooth transitions
        /// </summary>
        private void UpdateBannerAnimation() {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - _lastRepaintTime);
            _lastRepaintTime = currentTime;

            // Smooth animation speed
            float animationSpeed = 4f;
            _updateBannerAlpha = Mathf.MoveTowards(_updateBannerAlpha, _targetBannerAlpha, deltaTime * animationSpeed);

            // Continue repainting during animation
            if (Mathf.Abs(_updateBannerAlpha - _targetBannerAlpha) > 0.01f) {
                Repaint();
            }
        }

        /// <summary>
        /// Draws the update available notification banner
        /// UI Polish: Enhanced styling, tooltips, and confirmation dialogs
        /// </summary>
        private void DrawUpdateAvailableBanner() {
            var currentVersion = UpdateChecker.CurrentVersion;
            var latestVersion = UpdateChecker.LatestVersion;
            var lastCheckTime = UpdateChecker.LastCheckTime;

            // UI Polish: Create a colored background with AirConsole branding
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.3f); // Light green background

            EditorGUILayout.BeginVertical(_updateBannerStyle ?? EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // UI Polish: Header with icon and responsive layout
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔄 Update Available", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // UI Polish: Show update priority indicator
            var versionDiff = GetVersionDifference(currentVersion, latestVersion);
            if (versionDiff.isMajor) {
                EditorGUILayout.LabelField("Major Update", _helpTextStyle, GUILayout.Width(80));
            } else if (versionDiff.isMinor) {
                EditorGUILayout.LabelField("Minor Update", _helpTextStyle, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            // UI Polish: Responsive version display
            var windowWidth = position.width;
            if (windowWidth > 500) {
                // Wide layout: horizontal version display
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Current: v{currentVersion}", GUILayout.Width(150));
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                EditorGUILayout.LabelField($"Available: v{latestVersion}", GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            } else {
                // Narrow layout: vertical version display
                EditorGUILayout.LabelField($"Current: v{currentVersion}");
                EditorGUILayout.LabelField($"Available: v{latestVersion}");
            }

            EditorGUILayout.Space(5);

            // UI Polish: Action buttons with consistent styling and responsive layout
            EditorGUILayout.BeginHorizontal();

            // Primary action: Update Now with confirmation
            var updateButtonContent = new GUIContent("Update Now", "Download and install the latest version");
            if (GUILayout.Button(updateButtonContent, _primaryButtonStyle ?? GUI.skin.button, GUILayout.Width(100))) {
                ShowUpdateConfirmationDialog(latestVersion);
            }

            // Secondary action: View Release Notes
            var releaseNotesContent = new GUIContent("Release Notes", "View what's new in this version");
            if (GUILayout.Button(releaseNotesContent, _secondaryButtonStyle ?? GUI.skin.button, GUILayout.Width(110))) {
                UpdateChecker.OpenReleasePage();
            }

            // Tertiary action: Dismiss with confirmation
            var dismissContent = new GUIContent("Dismiss", "Hide this update notification");
            if (GUILayout.Button(dismissContent, _smallButtonStyle ?? GUI.skin.button, GUILayout.Width(70))) {
                ShowDismissConfirmationDialog(latestVersion);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // UI Polish: Help text with formatting
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("💡 Tip: Updates include bug fixes, new features, and performance improvements.",
                _helpTextStyle ?? EditorStyles.miniLabel);

            // Show last check time with better formatting
            if (lastCheckTime != DateTime.MinValue) {
                var timeText = FormatTimeSpan(DateTime.Now - lastCheckTime);
                EditorGUILayout.LabelField($"Last checked: {timeText} ago", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// UI Polish: Shows confirmation dialog for update installation
        /// This is the ONLY popup that should appear during the update process
        /// </summary>
        private void ShowUpdateConfirmationDialog(Version latestVersion) {
            var title = "Confirm Update Installation";
            var message = $"This will download and install AirConsole Plugin v{latestVersion}.\n\n" +
                         "The Unity Editor may need to restart to complete the installation.\n\n" +
                         "Do you want to continue?";

            if (EditorUtility.DisplayDialog(title, message, "Update Now", "Cancel")) {
                // Perform the actual update - we need to bypass the GithubUpdate confirmation
                PerformUpdateWithoutExtraDialogs();
            }
        }

        /// <summary>
        /// Performs the update by directly calling the GithubUpdate methods that bypass the confirmation
        /// </summary>
        private void PerformUpdateWithoutExtraDialogs() {
            try {
                // We need to call the update process but skip the ConfirmUpdate dialog
                // Let's create our own update flow that bypasses the GithubUpdate confirmation
                StartUpdateProcess();
            } catch (Exception ex) {
                // Only show error dialog if exception occurs
                EditorUtility.DisplayDialog("Update Error",
                    $"An error occurred during the update process:\n\n{ex.Message}\n\n" +
                    "Please try again later or download manually from GitHub.", "OK");
            }
        }

        /// <summary>
        /// Starts the update process using the method that bypasses the GithubUpdate confirmation dialog
        /// </summary>
        private void StartUpdateProcess() {
            // Use the new method that skips the GithubUpdate confirmation dialog
            // since we already showed our own confirmation dialog
            var success = UpdateChecker.DownloadAndInstallUpdateWithoutConfirmation();

            if (!success) {
                EditorUtility.DisplayDialog("Update Failed",
                    "Failed to download or install the update.\n\n" +
                    "Please try again later or download manually from GitHub.", "OK");
            }
        }

        /// <summary>
        /// UI Polish: Shows confirmation dialog for dismissing updates
        /// </summary>
        private void ShowDismissConfirmationDialog(Version latestVersion) {
            var title = "Dismiss Update Notification";
            var message = $"This will hide the update notification for v{latestVersion}.\n\n" +
                         "You can still manually check for updates in the settings below.\n\n" +
                         "The notification will reappear if a newer version becomes available.";

            if (EditorUtility.DisplayDialog(title, message, "Dismiss", "Cancel")) {
                UpdateChecker.DismissCurrentUpdate();
            }
        }

        /// <summary>
        /// UI Polish: Analyzes version difference for priority indication
        /// </summary>
        private (bool isMajor, bool isMinor, bool isPatch) GetVersionDifference(Version current, Version latest) {
            if (current == null || latest == null) {
                return (false, false, false);
            }

            bool isMajor = latest.Major > current.Major;
            bool isMinor = !isMajor && latest.Minor > current.Minor;
            bool isPatch = !isMajor && !isMinor && latest.Build > current.Build;

            return (isMajor, isMinor, isPatch);
        }

        /// <summary>
        /// Draws the check in progress banner
        /// UI Polish: Enhanced loading animation and styling
        /// </summary>
        private void DrawCheckInProgressBanner() {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 0.3f); // Light blue background

            EditorGUILayout.BeginVertical(_updateBannerStyle ?? EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔄 Checking for updates...", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // UI Polish: Enhanced loading indicator with smooth animation
            DrawLoadingIndicator();
            EditorGUILayout.EndHorizontal();

            // UI Polish: Progress information
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Connecting to GitHub API...", _helpTextStyle ?? EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            // Repaint to animate the loading indicator
            Repaint();
        }

        /// <summary>
        /// UI Polish: Draws an enhanced loading indicator
        /// </summary>
        private void DrawLoadingIndicator() {
            // Smooth rotating animation
            var time = (float)EditorApplication.timeSinceStartup;
            var rotationSpeed = 2f;
            var rotation = (time * rotationSpeed) % 1f;

            // Create spinning dots pattern
            var dots = new string[] { "●○○", "○●○", "○○●", "○●○" };
            var dotIndex = (int)(rotation * dots.Length) % dots.Length;

            EditorGUILayout.LabelField(dots[dotIndex], GUILayout.Width(30));
        }

        /// <summary>
        /// Draws the rate limiting status banner
        /// UI Polish: Enhanced styling and informative content
        /// </summary>
        private void DrawRateLimitingBanner() {
            var timeUntilNext = UpdateChecker.TimeUntilNextCheck();

            if (timeUntilNext == TimeSpan.Zero || timeUntilNext == TimeSpan.MaxValue) {
                return;
            }

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.3f); // Light orange background

            EditorGUILayout.BeginVertical(_updateBannerStyle ?? EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // UI Polish: Header with better formatting
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⏱️ Rate Limited", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            string timeText = FormatTimeSpan(timeUntilNext);
            EditorGUILayout.LabelField($"Next automatic check available in {timeText}", EditorStyles.label);

            var lastCheckTime = UpdateChecker.LastCheckTime;
            if (lastCheckTime != DateTime.MinValue) {
                var timeSinceCheck = DateTime.Now - lastCheckTime;
                string lastCheckText = FormatTimeSpan(timeSinceCheck);
                EditorGUILayout.LabelField($"Last checked: {lastCheckText} ago", EditorStyles.miniLabel);
            }

            // UI Polish: Helpful explanation
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("ℹ️ Rate limiting prevents excessive API requests to GitHub.",
                _helpTextStyle ?? EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the update settings section with preferences and controls
        /// UI Polish: Enhanced with tooltips, help text, and better organization
        /// </summary>
        private void DrawUpdateSettingsSection() {
            var settings = UpdateSettings.Instance;

            // UI Polish: Section container with proper header spacing
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // UI Polish: Collapsible section header inside container
            EditorGUILayout.BeginHorizontal();
            _showUpdateSettings = EditorGUILayout.Foldout(_showUpdateSettings, "Update Settings", true, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // UI Polish: Quick status indicator
            if (settings.AutomaticCheckEnabled) {
                EditorGUILayout.LabelField("✓ Enabled", _helpTextStyle, GUILayout.Width(60));
            } else {
                EditorGUILayout.LabelField("✗ Disabled", _helpTextStyle, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();

            if (!_showUpdateSettings) {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);

            // UI Polish: Automatic update checking toggle with tooltip
            var automaticCheckContent = new GUIContent(
                "Automatic Update Checking",
                "When enabled, the plugin will periodically check for updates from GitHub"
            );
            var newAutomaticEnabled = EditorGUILayout.Toggle(automaticCheckContent, settings.AutomaticCheckEnabled);
            if (newAutomaticEnabled != settings.AutomaticCheckEnabled) {
                settings.AutomaticCheckEnabled = newAutomaticEnabled;
                // Restart automatic checking to apply new settings
                UpdateChecker.RestartAutomaticChecking();
            }

            // UI Polish: Help text for automatic checking
            if (!settings.AutomaticCheckEnabled) {
                EditorGUILayout.LabelField("ℹ️ You can still check for updates manually using the button below.",
                    _helpTextStyle ?? EditorStyles.miniLabel);
            }

            // Check interval setting (only show if automatic checking is enabled)
            if (settings.AutomaticCheckEnabled) {
                EditorGUI.indentLevel++;

                // UI Polish: Check interval with tooltip and validation
                var intervalContent = new GUIContent(
                    "Check Interval (hours)",
                    "How often to check for updates. Minimum 24 hours to respect GitHub API limits."
                );
                var newInterval = EditorGUILayout.IntSlider(intervalContent, settings.CheckIntervalHours, 24, 168); // 24 hours to 1 week
                if (newInterval != settings.CheckIntervalHours) {
                    settings.CheckIntervalHours = newInterval;
                }

                // UI Polish: Show interval in human-readable format
                EditorGUILayout.LabelField($"  = Every {GetIntervalDescription(settings.CheckIntervalHours)}",
                    _helpTextStyle ?? EditorStyles.miniLabel);

                EditorGUILayout.Space(3);

                // UI Polish: Check on startup toggle with tooltip
                var startupContent = new GUIContent(
                    "Check on Editor Startup",
                    "Automatically check for updates when Unity Editor starts (respects rate limiting)"
                );
                var newCheckOnStartup = EditorGUILayout.Toggle(startupContent, settings.CheckOnStartup);
                if (newCheckOnStartup != settings.CheckOnStartup) {
                    settings.CheckOnStartup = newCheckOnStartup;
                }

                // UI Polish: Auto-open settings window toggle with tooltip
                var autoOpenContent = new GUIContent(
                    "Auto-open Settings Window",
                    "Automatically open this window when a new update is detected"
                );
                var newAutoOpenWindow = EditorGUILayout.Toggle(autoOpenContent, settings.AutoOpenSettingsWindow);
                if (newAutoOpenWindow != settings.AutoOpenSettingsWindow) {
                    settings.AutoOpenSettingsWindow = newAutoOpenWindow;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // UI Polish: Manual actions section with proper spacing
            EditorGUILayout.LabelField("Manual Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            var isCheckInProgress = UpdateChecker.IsCheckInProgress;

            // UI Polish: Check Now button with consistent styling
            GUI.enabled = !isCheckInProgress;
            var checkNowContent = new GUIContent("Check Now", "Immediately check for updates (bypasses rate limiting)");
            if (GUILayout.Button(checkNowContent, _primaryButtonStyle ?? GUI.skin.button, GUILayout.Width(100))) {
                UpdateChecker.CheckForUpdatesAsync(force: true); // Force manual checks to bypass rate limiting
                // Force UI refresh after a short delay to show results
                EditorApplication.delayCall += () => {
                    EditorApplication.delayCall += () => Repaint();
                };
            }
            GUI.enabled = true;

            // UI Polish: Show check status with better formatting and consistent spacing
            if (isCheckInProgress) {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Checking...", _helpTextStyle ?? EditorStyles.miniLabel, GUILayout.Width(60));
                DrawLoadingIndicator();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // UI Polish: Show dismissed updates button with consistent styling and spacing
            if (!UpdateChecker.IsUpdateAvailable && UpdateChecker.IsCurrentUpdateDismissed) {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                var showDismissedContent = new GUIContent(
                    "Show Dismissed Updates",
                    "Clear dismissed version to show update notifications again"
                );
                if (GUILayout.Button(showDismissedContent, _secondaryButtonStyle ?? GUI.skin.button, GUILayout.Width(180))) {
                    UpdateChecker.ClearDismissedVersion();
                    Repaint();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);

            // Display last check time and next available check time
            DrawUpdateTimingInfo();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// UI Polish: Converts check interval hours to human-readable description
        /// </summary>
        private string GetIntervalDescription(int hours) {
            if (hours < 24) return $"{hours} hours";
            if (hours == 24) return "day";
            if (hours == 48) return "2 days";
            if (hours == 72) return "3 days";
            if (hours == 168) return "week";

            var days = hours / 24;
            return $"{days} days";
        }

        /// <summary>
        /// Draws timing information for update checks
        /// </summary>
        private void DrawUpdateTimingInfo() {
            var lastCheckTime = UpdateChecker.LastCheckTime;
            var timeUntilNext = UpdateChecker.TimeUntilNextCheck();

            EditorGUILayout.BeginVertical();

            // Last check time
            if (lastCheckTime != DateTime.MinValue) {
                var timeSinceCheck = DateTime.Now - lastCheckTime;
                string lastCheckText = FormatTimeSpan(timeSinceCheck);
                EditorGUILayout.LabelField($"Last checked: {lastCheckText} ago", EditorStyles.miniLabel);
            } else {
                EditorGUILayout.LabelField("Last checked: Never", EditorStyles.miniLabel);
            }

            // Next automatic check time (only if automatic checking is enabled)
            var settings = UpdateSettings.Instance;
            if (settings.AutomaticCheckEnabled) {
                if (timeUntilNext == TimeSpan.Zero) {
                    EditorGUILayout.LabelField("Next automatic check: Available now", EditorStyles.miniLabel);
                } else if (timeUntilNext != TimeSpan.MaxValue) {
                    string nextCheckText = FormatTimeSpan(timeUntilNext);
                    EditorGUILayout.LabelField($"Next automatic check: In {nextCheckText}", EditorStyles.miniLabel);
                }
            } else {
                EditorGUILayout.LabelField("Next automatic check: Disabled", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
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

            // Update Settings Section
            DrawUpdateSettingsSection();

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
                updateSettings.CheckIntervalHours = 24;
                updateSettings.CheckOnStartup = true;
                updateSettings.AutoOpenSettingsWindow = true;
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