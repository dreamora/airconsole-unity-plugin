#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System;

namespace NDream.AirConsole.Editor {
    /// <summary>
    /// Dedicated editor window for AirConsole plugin update checking functionality.
    /// Provides a focused interface for managing updates, settings, and status.
    /// </summary>
    public class UpdateCheckerWindow : EditorWindow {
        private static Texture2D bg;
        private static Texture logo;
        private static Texture logoSmall;
        private static GUIContent titleInfo;

        // UI Polish: Animation and styling variables
        private static float _updateBannerAlpha = 0f;
        private static float _targetBannerAlpha = 0f;
        private static double _lastRepaintTime = 0;
        private static Vector2 _scrollPosition = Vector2.zero;

        // UI Polish: Cached GUIStyles for consistent branding
        private GUIStyle _updateBannerStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _smallButtonStyle;
        private GUIStyle _helpTextStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized = false;

        [MenuItem("Window/AirConsole/Update Checker")]
        public static void ShowWindow() {
            UpdateCheckerWindow window = GetWindow<UpdateCheckerWindow>("AirConsole Update Checker");
            window.titleContent = new GUIContent("Update Checker", logoSmall, "AirConsole Update Checker");
            window.minSize = new Vector2(400, 300);
            window.Show();
            window.Focus();
        }

        private void OnEnable() {
            // Load images
            bg = (Texture2D)Resources.Load("AirConsoleBg");
            logo = (Texture)Resources.Load("AirConsoleLogoText");
            logoSmall = (Texture)Resources.Load("AirConsoleLogoSmall");
            titleInfo = new GUIContent("AirConsole Update Checker", logoSmall, "AirConsole Update Checker");

            // Set minimum window size for responsive layout
            minSize = new Vector2(400, 300);

            // Reset style initialization flag so styles are recreated in OnGUI
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

                // Status style for version and status information
                _statusStyle = new GUIStyle(EditorStyles.label) {
                    fontSize = 12,
                    margin = new RectOffset(0, 0, 2, 2)
                };

                _stylesInitialized = true;
            } catch (System.ArgumentException) {
                // GUI functions not available yet, styles will be initialized on next OnGUI call
                _stylesInitialized = false;
            }
        }

        private void OnGUI() {
            // UI Polish: Initialize styles if needed
            InitializeCustomStyles();

            // UI Polish: Responsive layout with scrolling for smaller windows
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawCurrentStatus();
            DrawUpdateActions();
            DrawUpdateSettings();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the header with AirConsole branding
        /// </summary>
        private void DrawHeader() {
            // Create black background style for header
            var styleBlack = new GUIStyle {
                normal = { background = bg, textColor = Color.white },
                margin = { top = 5 },
                padding = { right = 5 }
            };

            // Show logo & version
            EditorGUILayout.BeginHorizontal(styleBlack, GUILayout.Height(30));
            if (logo != null) {
                GUILayout.Label(logo, GUILayout.Width(128), GUILayout.Height(30));
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("Update Checker", styleBlack);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the current status section showing version information and update availability
        /// </summary>
        private void DrawCurrentStatus() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Current Status", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var currentVersion = UpdateChecker.CurrentVersion;
            var latestVersion = UpdateChecker.LatestVersion;
            var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
            var isCheckInProgress = UpdateChecker.IsCheckInProgress;

            // Current version
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Version:", GUILayout.Width(120));
            EditorGUILayout.LabelField($"v{currentVersion}", _statusStyle ?? EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // Latest version
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latest Version:", GUILayout.Width(120));
            if (latestVersion != null) {
                EditorGUILayout.LabelField($"v{latestVersion}", _statusStyle ?? EditorStyles.label);
            } else {
                EditorGUILayout.LabelField("Unknown", _statusStyle ?? EditorStyles.label);
            }
            EditorGUILayout.EndHorizontal();

            // Status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(120));

            if (isCheckInProgress) {
                EditorGUILayout.LabelField("Checking for updates...", _statusStyle ?? EditorStyles.label);
                DrawLoadingIndicator();
            } else if (isUpdateAvailable) {
                var originalColor = GUI.color;
                GUI.color = new Color(0.2f, 0.8f, 0.2f); // Green color for update available
                EditorGUILayout.LabelField("✓ Update Available", _statusStyle ?? EditorStyles.label);
                GUI.color = originalColor;
            } else if (latestVersion != null) {
                var originalColor = GUI.color;
                GUI.color = new Color(0.2f, 0.6f, 0.8f); // Blue color for up-to-date
                EditorGUILayout.LabelField("✓ Up to Date", _statusStyle ?? EditorStyles.label);
                GUI.color = originalColor;
            } else {
                EditorGUILayout.LabelField("Unknown", _statusStyle ?? EditorStyles.label);
            }
            EditorGUILayout.EndHorizontal();

            // Last check time
            var lastCheckTime = UpdateChecker.LastCheckTime;
            if (lastCheckTime != DateTime.MinValue) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Last Checked:", GUILayout.Width(120));
                var timeText = FormatTimeSpan(DateTime.Now - lastCheckTime);
                EditorGUILayout.LabelField($"{timeText} ago", _helpTextStyle ?? EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Rate limiting status
            var timeUntilNext = UpdateChecker.TimeUntilNextCheck();
            if (timeUntilNext > TimeSpan.Zero && timeUntilNext != TimeSpan.MaxValue) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Next Check:", GUILayout.Width(120));
                var nextCheckText = FormatTimeSpan(timeUntilNext);
                EditorGUILayout.LabelField($"In {nextCheckText}", _helpTextStyle ?? EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            // Repaint to animate the loading indicator
            if (isCheckInProgress) {
                Repaint();
            }
        }

        /// <summary>
        /// Draws the update actions section with buttons for update management
        /// </summary>
        private void DrawUpdateActions() {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Update Actions", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
            var isCheckInProgress = UpdateChecker.IsCheckInProgress;
            var latestVersion = UpdateChecker.LatestVersion;

            if (isUpdateAvailable) {
                // Update available section
                DrawUpdateAvailableActions(latestVersion);
            } else {
                // No update available section
                DrawNoUpdateActions();
            }

            EditorGUILayout.Space(8);

            // Manual check section
            EditorGUILayout.LabelField("Manual Check", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !isCheckInProgress;
            var checkNowContent = new GUIContent("Check Now", "Immediately check for updates");
            if (GUILayout.Button(checkNowContent, _primaryButtonStyle ?? GUI.skin.button, GUILayout.Width(100))) {
                UpdateChecker.CheckForUpdatesAsync(force: true);
                // Force UI refresh after a short delay to show results
                EditorApplication.delayCall += () => {
                    EditorApplication.delayCall += () => Repaint();
                };
            }
            GUI.enabled = true;

            if (isCheckInProgress) {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Checking...", _helpTextStyle ?? EditorStyles.miniLabel, GUILayout.Width(60));
                DrawLoadingIndicator();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Show dismissed updates button
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

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        /// <summary>
        /// Draws the actions available when an update is available
        /// </summary>
        private void DrawUpdateAvailableActions(Version latestVersion) {
            var currentVersion = UpdateChecker.CurrentVersion;

            // Version comparison display
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

            // Update priority indicator
            var versionDiff = GetVersionDifference(currentVersion, latestVersion);
            if (versionDiff.isMajor) {
                EditorGUILayout.LabelField("🔴 Major Update - Recommended", _helpTextStyle ?? EditorStyles.miniLabel);
            } else if (versionDiff.isMinor) {
                EditorGUILayout.LabelField("🟡 Minor Update - New features available", _helpTextStyle ?? EditorStyles.miniLabel);
            } else {
                EditorGUILayout.LabelField("🟢 Patch Update - Bug fixes available", _helpTextStyle ?? EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8);

            // Action buttons
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

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("💡 Tip: Updates include bug fixes, new features, and performance improvements.",
                _helpTextStyle ?? EditorStyles.miniLabel);
        }

        /// <summary>
        /// Draws the actions available when no update is available
        /// </summary>
        private void DrawNoUpdateActions() {
            EditorGUILayout.LabelField("✓ You have the latest version installed.", _statusStyle ?? EditorStyles.label);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("💡 Check back later for new updates, or enable automatic checking below.",
                _helpTextStyle ?? EditorStyles.miniLabel);
        }

        /// <summary>
        /// Draws the update settings section
        /// </summary>
        private void DrawUpdateSettings() {
            var settings = UpdateSettings.Instance;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Update Settings", _sectionHeaderStyle ?? EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Automatic update checking toggle
            var automaticCheckContent = new GUIContent(
                "Automatic Update Checking",
                "When enabled, the plugin will periodically check for updates from GitHub"
            );
            var newAutomaticEnabled = EditorGUILayout.Toggle(automaticCheckContent, settings.AutomaticCheckEnabled);
            if (newAutomaticEnabled != settings.AutomaticCheckEnabled) {
                settings.AutomaticCheckEnabled = newAutomaticEnabled;
                UpdateChecker.RestartAutomaticChecking();
            }

            if (!settings.AutomaticCheckEnabled) {
                EditorGUILayout.LabelField("ℹ️ You can still check for updates manually using the button above.",
                    _helpTextStyle ?? EditorStyles.miniLabel);
            }

            // Check interval setting (only show if automatic checking is enabled)
            if (settings.AutomaticCheckEnabled) {
                EditorGUI.indentLevel++;

                var intervalContent = new GUIContent(
                    "Check Interval (hours)",
                    "How often to check for updates. Minimum 24 hours to respect GitHub API limits."
                );
                var newInterval = EditorGUILayout.IntSlider(intervalContent, settings.CheckIntervalHours, 24, 168); // 24 hours to 1 week
                if (newInterval != settings.CheckIntervalHours) {
                    settings.CheckIntervalHours = newInterval;
                }

                EditorGUILayout.LabelField($"  = Every {GetIntervalDescription(settings.CheckIntervalHours)}",
                    _helpTextStyle ?? EditorStyles.miniLabel);

                EditorGUILayout.Space(3);

                var startupContent = new GUIContent(
                    "Check on Editor Startup",
                    "Automatically check for updates when Unity Editor starts (respects rate limiting)"
                );
                var newCheckOnStartup = EditorGUILayout.Toggle(startupContent, settings.CheckOnStartup);
                if (newCheckOnStartup != settings.CheckOnStartup) {
                    settings.CheckOnStartup = newCheckOnStartup;
                }

                var autoOpenContent = new GUIContent(
                    "Auto-open Update Checker Window",
                    "Automatically open this Update Checker window when a new update is detected"
                );
                var newAutoOpenWindow = EditorGUILayout.Toggle(autoOpenContent, settings.AutoOpenSettingsWindow);
                if (newAutoOpenWindow != settings.AutoOpenSettingsWindow) {
                    settings.AutoOpenSettingsWindow = newAutoOpenWindow;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // Diagnostic information
            if (settings.FailedCheckCount > 0) {
                EditorGUILayout.LabelField("Diagnostic Information", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);

                var diagnosticInfo = settings.GetDiagnosticInfo();
                EditorGUILayout.LabelField(diagnosticInfo, _helpTextStyle ?? EditorStyles.miniLabel);

                EditorGUILayout.Space(5);
                if (GUILayout.Button("Reset Error State", _secondaryButtonStyle ?? GUI.skin.button, GUILayout.Width(120))) {
                    UpdateChecker.ResetErrorState();
                    Repaint();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Shows confirmation dialog for update installation
        /// </summary>
        private void ShowUpdateConfirmationDialog(Version latestVersion) {
            var title = "Confirm Update Installation";
            var message = $"This will download and install AirConsole Plugin v{latestVersion}.\n\n" +
                         "The Unity Editor may need to restart to complete the installation.\n\n" +
                         "Do you want to continue?";

            if (EditorUtility.DisplayDialog(title, message, "Update Now", "Cancel")) {
                PerformUpdateWithoutExtraDialogs();
            }
        }

        /// <summary>
        /// Performs the update by directly calling the UpdateChecker methods
        /// </summary>
        private void PerformUpdateWithoutExtraDialogs() {
            try {
                var success = UpdateChecker.DownloadAndInstallUpdateWithoutConfirmation();
                if (!success) {
                    EditorUtility.DisplayDialog("Update Failed",
                        "Failed to download or install the update.\n\n" +
                        "Please try again later or download manually from GitHub.", "OK");
                }
            } catch (Exception ex) {
                EditorUtility.DisplayDialog("Update Error",
                    $"An error occurred during the update process:\n\n{ex.Message}\n\n" +
                    "Please try again later or download manually from GitHub.", "OK");
            }
        }

        /// <summary>
        /// Shows confirmation dialog for dismissing updates
        /// </summary>
        private void ShowDismissConfirmationDialog(Version latestVersion) {
            var title = "Dismiss Update Notification";
            var message = $"This will hide the update notification for v{latestVersion}.\n\n" +
                         "You can still manually check for updates in this window.\n\n" +
                         "The notification will reappear if a newer version becomes available.";

            if (EditorUtility.DisplayDialog(title, message, "Dismiss", "Cancel")) {
                UpdateChecker.DismissCurrentUpdate();
                Repaint();
            }
        }

        /// <summary>
        /// Analyzes version difference for priority indication
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
        /// Draws an enhanced loading indicator
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
        /// Converts check interval hours to human-readable description
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
        /// Formats a TimeSpan into a human-readable string
        /// </summary>
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
    }
}
#endif