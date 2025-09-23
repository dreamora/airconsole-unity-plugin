#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Main API interface for the AirConsole plugin update checking system.
    /// Provides automatic checking initialization, scheduling logic, manual check functionality,
    /// and dismiss functionality for specific update versions.
    /// </summary>
    public static class UpdateChecker {
        private static bool _automaticCheckingInitialized;
        private static bool _isScheduledCheckPending;
        private static bool _lastUpdateAvailableState;
        private static Version _lastNotifiedVersion;

        #region Public Properties

        /// <summary>
        /// Gets whether an update is currently available
        /// </summary>
        public static bool IsUpdateAvailable => GithubUpdate.IsUpdateAvailable && !IsCurrentUpdateDismissed;

        /// <summary>
        /// Gets the latest available version from GitHub
        /// </summary>
        public static Version LatestVersion => GithubUpdate.LatestVersion;

        /// <summary>
        /// Gets whether an update check is currently in progress
        /// </summary>
        public static bool IsCheckInProgress => GithubUpdate.IsCheckInProgress;

        /// <summary>
        /// Gets the time of the last update check
        /// </summary>
        public static DateTime LastCheckTime => GithubUpdate.LastCheckTime;

        /// <summary>
        /// Gets the current version of the plugin
        /// </summary>
        public static Version CurrentVersion => GithubUpdate.GetCurrentVersion();

        /// <summary>
        /// Gets whether the current available update has been dismissed by the user
        /// </summary>
        public static bool IsCurrentUpdateDismissed {
            get {
                var latestVersion = LatestVersion;
                return latestVersion != null && GithubUpdate.IsVersionDismissed(latestVersion);
            }
        }

        #endregion

        #region Automatic Checking

        /// <summary>
        /// Starts the automatic update checking system.
        /// This should be called once during editor initialization.
        /// </summary>
        public static void StartAutomaticChecking() {
            if (_automaticCheckingInitialized) {
                AirConsoleLogger.Log(() => "UpdateChecker automatic checking already initialized");
                return;
            }

            var settings = UpdateSettings.Instance;
            if (!settings.AutomaticCheckEnabled) {
                AirConsoleLogger.Log(() => "UpdateChecker automatic checking disabled in settings");
                return;
            }

            _automaticCheckingInitialized = true;
            AirConsoleLogger.Log(() => "UpdateChecker automatic checking system started");

            // Check on startup if enabled - always allow first startup check to bypass rate limiting
            if (settings.CheckOnStartup) {
                AirConsoleLogger.Log(() => "Scheduling startup update check (bypassing rate limiting for editor startup)");
                EditorApplication.delayCall += () => {
                    // Force startup check to bypass rate limiting - this is the first check after editor startup
                    CheckForUpdatesAsync(force: true);
                };
            } else {
                AirConsoleLogger.Log(() => "Startup update check disabled in settings");
            }

            // Set up periodic checking
            EditorApplication.update += PeriodicCheckHandler;
        }

        /// <summary>
        /// Stops the automatic update checking system.
        /// This should be called when shutting down or when automatic checking is disabled.
        /// </summary>
        public static void StopAutomaticChecking() {
            if (!_automaticCheckingInitialized) {
                return;
            }

            AirConsoleLogger.Log(() => "UpdateChecker automatic checking system stopped");
            _automaticCheckingInitialized = false;
            _isScheduledCheckPending = false;
            EditorApplication.update -= PeriodicCheckHandler;
        }

        /// <summary>
        /// Restarts automatic checking with current settings.
        /// Useful when settings change.
        /// </summary>
        public static void RestartAutomaticChecking() {
            StopAutomaticChecking();
            StartAutomaticChecking();
        }

        #endregion

        #region Manual Checking

        /// <summary>
        /// Manually triggers an update check.
        /// Respects rate limiting rules unless force is specified.
        /// </summary>
        /// <param name="force">If true, bypasses rate limiting (use with caution)</param>
        /// <returns>True if the check was started, false if rate limited or already in progress</returns>
        public static bool CheckForUpdatesAsync(bool force = false) {
            var settings = UpdateSettings.Instance;

            // Always respect rate limiting for manual checks unless explicitly forced
            if (!force && !settings.CanCheckNow()) {
                var timeUntilNext = settings.TimeUntilNextCheck();
                AirConsoleLogger.LogWarning(() => $"Update check rate limited. Next check available in {timeUntilNext:hh\\:mm\\:ss}");
                return false;
            }

            if (force) {
                AirConsoleLogger.Log(() => "Update check initiated (rate limiting bypassed)");
            } else {
                AirConsoleLogger.Log(() => "Update check initiated");
            }

            // Use the enhanced GithubUpdate method with rate limiting
            return GithubUpdate.BeginBackgroundUpdateCheck(force);
        }

        /// <summary>
        /// Gets whether an update check can be performed now based on rate limiting
        /// </summary>
        /// <returns>True if a check can be performed now</returns>
        public static bool CanCheckNow() {
            return GithubUpdate.CanCheckNow();
        }

        /// <summary>
        /// Gets the time until the next check is allowed
        /// </summary>
        /// <returns>TimeSpan until next check, or TimeSpan.Zero if check is allowed now</returns>
        public static TimeSpan TimeUntilNextCheck() {
            return GithubUpdate.TimeUntilNextCheck();
        }



        #endregion

        #region Dismiss Functionality

        /// <summary>
        /// Dismisses the current available update so it won't show notifications.
        /// The update will reappear if a newer version becomes available.
        /// </summary>
        public static void DismissCurrentUpdate() {
            var latestVersion = LatestVersion;
            if (latestVersion != null) {
                GithubUpdate.DismissVersion(latestVersion);
                AirConsoleLogger.Log(() => $"Update v{latestVersion} dismissed. Notifications will not show until a newer version is available.");

                // Reset notification state since user dismissed this version
                _lastUpdateAvailableState = false;
            }
        }

        /// <summary>
        /// Dismisses a specific version so it won't show notifications.
        /// </summary>
        /// <param name="version">The version to dismiss</param>
        public static void DismissVersion(Version version) {
            if (version != null) {
                GithubUpdate.DismissVersion(version);
                AirConsoleLogger.Log(() => $"Update v{version} dismissed.");
            }
        }

        /// <summary>
        /// Clears any dismissed version, allowing all updates to show notifications again.
        /// </summary>
        public static void ClearDismissedVersion() {
            UpdateSettings.Instance.DismissedVersion = "";
            AirConsoleLogger.Log(() => "Dismissed version cleared. All future updates will show notifications.");

            // Reset notification state so that if an update becomes available again, it will show
            _lastNotifiedVersion = null;
            _lastUpdateAvailableState = false;
        }

        /// <summary>
        /// Checks if a specific version is dismissed
        /// </summary>
        /// <param name="version">Version to check</param>
        /// <returns>True if the version is dismissed</returns>
        public static bool IsVersionDismissed(Version version) {
            return GithubUpdate.IsVersionDismissed(version);
        }

        #endregion

        #region Update Installation

        /// <summary>
        /// Downloads and installs the latest plugin update from GitHub.
        /// Shows progress dialog and confirmation prompts to the user.
        /// </summary>
        /// <returns>True if the update process was initiated successfully</returns>
        public static bool DownloadAndInstallUpdate() {
            try {
                GithubUpdate.TryUpdatePluginFromGithub();
                AirConsoleLogger.Log(() => "Update download and installation initiated.");
                return true;
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Failed to initiate update download: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Opens the GitHub releases page in the default browser as a fallback option
        /// </summary>
        public static void OpenReleasePage() {
            const string releasesUrl = "https://github.com/airconsole/airconsole-unity-plugin/releases";
            Application.OpenURL(releasesUrl);
            AirConsoleLogger.Log(() => "Opened GitHub releases page in browser.");
        }

        /// <summary>
        /// Opens the specific release page for the latest version if available
        /// </summary>
        public static void OpenLatestReleasePage() {
            // If we have cached release info with a direct URL, use that
            // Otherwise fall back to the general releases page
            OpenReleasePage();
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Handles periodic checking logic
        /// </summary>
        private static void PeriodicCheckHandler() {
            if (!_automaticCheckingInitialized) {
                return;
            }

            var settings = UpdateSettings.Instance;

            // Stop if automatic checking is disabled
            if (!settings.AutomaticCheckEnabled) {
                StopAutomaticChecking();
                return;
            }

            // Check for update status changes and handle notifications
            CheckForUpdateStatusChanges();

            // Skip if check is already in progress
            if (IsCheckInProgress) {
                return;
            }

            // Check if it's time for a scheduled check
            if (!_isScheduledCheckPending && settings.CanCheckNow()) {
                _isScheduledCheckPending = true;

                // Schedule the check for the next editor update to avoid blocking
                EditorApplication.delayCall += () => {
                    _isScheduledCheckPending = false;
                    CheckForUpdatesAsync();
                };
            }
        }

        /// <summary>
        /// Checks for changes in update availability and handles notifications
        /// </summary>
        private static void CheckForUpdateStatusChanges() {
            var currentUpdateAvailable = IsUpdateAvailable;
            var currentLatestVersion = LatestVersion;
            var settings = UpdateSettings.Instance;

            // Check if update status changed from false to true (new update detected)
            if (!_lastUpdateAvailableState && currentUpdateAvailable) {
                // Check if this is a new version we haven't notified about yet
                if (currentLatestVersion != null &&
                    (_lastNotifiedVersion == null || currentLatestVersion > _lastNotifiedVersion)) {

                    AirConsoleLogger.Log(() => $"New update detected: v{currentLatestVersion}.");

                    // Auto-open settings window if enabled
                    if (settings.AutoOpenSettingsWindow) {
                        AirConsoleLogger.Log(() => "Auto-opening AirConsole Settings window to show update notification.");
                        EditorApplication.delayCall += () => {
                            SettingWindow.OpenSettingsWindow();
                        };
                    } else {
                        AirConsoleLogger.Log(() => "Auto-open settings window is disabled. Update notification available in AirConsole Settings.");
                    }

                    // Remember this version so we don't repeatedly open the window
                    _lastNotifiedVersion = currentLatestVersion;
                }
            }

            // Update the last known state
            _lastUpdateAvailableState = currentUpdateAvailable;
        }

        #endregion

        #region Editor Initialization

        /// <summary>
        /// Initialize the update checker when the editor starts
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad() {
            // Delay initialization to ensure all systems are ready
            EditorApplication.delayCall += () => {
                try {
                    // Initialize UpdateSettings first to ensure asset creation
                    var settings = UpdateSettings.Instance;

                    // Log startup initialization for debugging
                    AirConsoleLogger.Log(() => $"UpdateChecker initializing on editor startup. AutoCheck: {settings.AutomaticCheckEnabled}, CheckOnStartup: {settings.CheckOnStartup}");

                    // Start automatic checking system
                    StartAutomaticChecking();
                } catch (Exception ex) {
                    AirConsoleLogger.LogError(() => $"Failed to initialize UpdateChecker on startup: {ex.Message}");
                }
            };
        }

        #endregion
    }
}
#endif