using System.Text;
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
        public static bool IsUpdateAvailable {
            get => GithubUpdate.IsUpdateAvailable && !IsCurrentUpdateDismissed;
        }

        /// <summary>
        /// Gets the latest available version from GitHub
        /// </summary>
        public static Version LatestVersion {
            get => GithubUpdate.LatestVersion;
        }

        /// <summary>
        /// Gets whether an update check is currently in progress
        /// </summary>
        public static bool IsCheckInProgress {
            get => GithubUpdate.IsCheckInProgress;
        }

        /// <summary>
        /// Gets the time of the last update check
        /// </summary>
        public static DateTime LastCheckTime {
            get => GithubUpdate.LastCheckTime;
        }

        /// <summary>
        /// Gets the current version of the plugin
        /// </summary>
        public static Version CurrentVersion {
            get => GithubUpdate.GetCurrentVersion();
        }

        /// <summary>
        /// Gets whether the current available update has been dismissed by the user
        /// </summary>
        public static bool IsCurrentUpdateDismissed {
            get {
                Version latestVersion = LatestVersion;
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

            UpdateSettings settings = UpdateSettings.Instance;
            if (!settings.AutomaticCheckEnabled) {
                AirConsoleLogger.Log(() => "UpdateChecker automatic checking disabled in settings");
                return;
            }

            // Check if automatic checking should be disabled due to persistent failures
            if (ShouldDisableAutomaticChecking()) {
                AirConsoleLogger.LogWarning(() => "Automatic checking disabled due to persistent failures. Use 'Reset Error State' to re-enable.");
                settings.AutomaticCheckEnabled = false;
                return;
            }

            _automaticCheckingInitialized = true;
            AirConsoleLogger.Log(() =>
                $"UpdateChecker automatic checking system started (interval: {settings.CheckIntervalHours}h, failures: {settings.FailedCheckCount})");

            // Check on startup if enabled - always allow first startup check to bypass rate limiting
            if (settings.CheckOnStartup) {
                AirConsoleLogger.Log(() => "Scheduling startup update check (bypassing rate limiting for editor startup)");
                EditorApplication.delayCall += () => {
                    // Force startup check to bypass rate limiting - this is the first check after editor startup
                    CheckForUpdatesAsync(true);
                };
            } else {
                AirConsoleLogger.Log(() => "Startup update check disabled in settings");
            }

            // Set up periodic checking with intelligent scheduling
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

            // Clean up periodic checking
            EditorApplication.update -= PeriodicCheckHandler;

            // Reset state
            _automaticCheckingInitialized = false;
            _isScheduledCheckPending = false;

            // Cancel any pending delayed calls for update checks
            // Note: We can't directly remove specific delayed calls, but we check state in the callback
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
            UpdateSettings settings = UpdateSettings.Instance;

            // Check if we should attempt recovery from error state
            if (!force && settings.ShouldAttemptRecovery()) {
                AirConsoleLogger.Log(() => "Attempting recovery from previous error state");
                force = true; // Allow recovery attempt
            }

            // Always respect rate limiting for manual checks unless explicitly forced
            if (!force && !settings.CanCheckNow()) {
                TimeSpan timeUntilNext = settings.TimeUntilNextCheck();
                string rateLimitMessage = $"Update check rate limited. Next check available in {timeUntilNext:hh\\:mm\\:ss}";

                // Add diagnostic info if there are previous errors
                if (settings.FailedCheckCount > 0) {
                    rateLimitMessage += $" ({settings.GetDiagnosticInfo()})";
                }

                AirConsoleLogger.LogWarning(() => rateLimitMessage);
                return false;
            }

            if (force) {
                AirConsoleLogger.Log(() => "Update check initiated (rate limiting bypassed)");
            } else {
                AirConsoleLogger.Log(() => "Update check initiated");
            }

            // Use the enhanced GithubUpdate method with rate limiting
            bool result = GithubUpdate.BeginBackgroundUpdateCheck(force);

            if (!result) {
                AirConsoleLogger.LogWarning(() => "Failed to start update check - may be rate limited or already in progress");
            }

            return result;
        }

        /// <summary>
        /// Gets whether an update check can be performed now based on rate limiting
        /// </summary>
        /// <returns>True if a check can be performed now</returns>
        public static bool CanCheckNow() => GithubUpdate.CanCheckNow();

        /// <summary>
        /// Gets the time until the next check is allowed
        /// </summary>
        /// <returns>TimeSpan until next check, or TimeSpan.Zero if check is allowed now</returns>
        public static TimeSpan TimeUntilNextCheck() => GithubUpdate.TimeUntilNextCheck();

        /// <summary>
        /// Validates and enforces check interval constraints
        /// </summary>
        /// <param name="requestedIntervalHours">The requested check interval in hours</param>
        /// <returns>The validated interval (fixed at 12 hours)</returns>
        public static int ValidateCheckInterval(int requestedIntervalHours) {
            const int fixedInterval = 12; // Fixed 12-hour interval

            // Always return the fixed interval regardless of input
            return fixedInterval;
        }

        /// <summary>
        /// Gets the current effective check interval considering backoff
        /// </summary>
        /// <returns>The effective check interval in hours</returns>
        public static int GetEffectiveCheckInterval() {
            UpdateSettings settings = UpdateSettings.Instance;
            return settings.CheckIntervalHours;
        }

        /// <summary>
        /// Checks if the automatic checking system should be disabled due to persistent failures
        /// </summary>
        /// <returns>True if automatic checking should be disabled</returns>
        public static bool ShouldDisableAutomaticChecking() {
            UpdateSettings settings = UpdateSettings.Instance;

            // Disable after 5 consecutive failures
            if (settings.FailedCheckCount >= 5) {
                return true;
            }

            // Disable if we've had network errors for more than 7 days
            if (settings.NetworkErrorDetected && settings.LastErrorTime != DateTime.MinValue) {
                TimeSpan timeSinceError = DateTime.Now - settings.LastErrorTime;
                if (timeSinceError.TotalDays > 7) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the status of the periodic checking system
        /// </summary>
        /// <returns>Status information about the periodic checking system</returns>
        public static string GetPeriodicCheckingStatus() {
            UpdateSettings settings = UpdateSettings.Instance;
            StringBuilder status = new();

            status.AppendLine($"Automatic Checking: {(settings.AutomaticCheckEnabled ? "Enabled" : "Disabled")}");
            status.AppendLine($"System Initialized: {_automaticCheckingInitialized}");
            status.AppendLine($"Check Interval: {settings.CheckIntervalHours} hours");
            status.AppendLine($"Failed Check Count: {settings.FailedCheckCount}");

            if (settings.FailedCheckCount > 0) {
                int effectiveInterval = GetEffectiveCheckInterval();
                status.AppendLine($"Effective Interval (with backoff): {effectiveInterval} hours");
            }

            TimeSpan timeUntilNext = TimeUntilNextCheck();
            if (timeUntilNext > TimeSpan.Zero) {
                status.AppendLine($"Next Check Available In: {timeUntilNext:hh\\:mm\\:ss}");
            } else {
                status.AppendLine("Next Check: Available now");
            }

            if (_isScheduledCheckPending) {
                status.AppendLine("Status: Check scheduled for next update cycle");
            } else if (IsCheckInProgress) {
                status.AppendLine("Status: Check in progress");
            } else {
                status.AppendLine("Status: Idle");
            }

            return status.ToString();
        }

        /// <summary>
        /// Forces a restart of the periodic checking system (useful for testing or recovery)
        /// </summary>
        public static void ForceRestartPeriodicChecking() {
            AirConsoleLogger.Log(() => "Force restarting periodic checking system");
            StopAutomaticChecking();

            // Small delay to ensure cleanup is complete
            EditorApplication.delayCall += () => { StartAutomaticChecking(); };
        }
        #endregion

        #region Dismiss Functionality
        /// <summary>
        /// Dismisses the current available update so it won't show notifications.
        /// The update will reappear if a newer version becomes available.
        /// </summary>
        public static void DismissCurrentUpdate() {
            Version latestVersion = LatestVersion;
            if (latestVersion != null) {
                GithubUpdate.DismissVersion(latestVersion);
                AirConsoleLogger.Log(() =>
                    $"Update v{latestVersion} dismissed. Notifications will not show until a newer version is available.");

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
        /// Resets the error state and re-enables automatic checking
        /// </summary>
        public static void ResetErrorState() {
            UpdateSettings settings = UpdateSettings.Instance;
            settings.ResetFailedCheckCount();

            if (!settings.AutomaticCheckEnabled) {
                settings.AutomaticCheckEnabled = true;
                AirConsoleLogger.Log(() => "Automatic update checking re-enabled after error state reset");

                // Restart automatic checking
                RestartAutomaticChecking();
            }

            AirConsoleLogger.Log(() => "Update checker error state reset successfully");
        }

        /// <summary>
        /// Gets diagnostic information about the current update checker state
        /// </summary>
        /// <returns>Formatted diagnostic string</returns>
        public static string GetDiagnosticInfo() {
            UpdateSettings settings = UpdateSettings.Instance;
            StringBuilder info = new();

            info.AppendLine($"Update Available: {IsUpdateAvailable}");
            info.AppendLine($"Check In Progress: {IsCheckInProgress}");
            info.AppendLine($"Automatic Checking: {settings.AutomaticCheckEnabled}");
            info.AppendLine(
                $"Last Check: {(LastCheckTime == DateTime.MinValue ? "Never" : LastCheckTime.ToString("yyyy-MM-dd HH:mm:ss"))}");
            info.AppendLine($"Current Version: {CurrentVersion}");
            info.AppendLine($"Latest Version: {LatestVersion?.ToString() ?? "Unknown"}");
            info.AppendLine($"Failed Check Count: {settings.FailedCheckCount}");

            if (settings.FailedCheckCount > 0) {
                info.AppendLine($"Error Details: {settings.GetDiagnosticInfo()}");
            }

            TimeSpan timeUntilNext = TimeUntilNextCheck();
            if (timeUntilNext > TimeSpan.Zero) {
                info.AppendLine($"Next Check Available: {timeUntilNext:hh\\:mm\\:ss}");
            } else {
                info.AppendLine("Next Check: Available now");
            }

            return info.ToString();
        }

        /// <summary>
        /// Checks if a specific version is dismissed
        /// </summary>
        /// <param name="version">Version to check</param>
        /// <returns>True if the version is dismissed</returns>
        public static bool IsVersionDismissed(Version version) => GithubUpdate.IsVersionDismissed(version);
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
        /// Downloads and installs the latest plugin update from GitHub without showing confirmation dialog.
        /// Use this when confirmation has already been obtained from the UI.
        /// </summary>
        /// <returns>True if the update process was initiated successfully</returns>
        public static bool DownloadAndInstallUpdateWithoutConfirmation() {
            try {
                GithubUpdate.UpdatePluginFromGithubWithoutConfirmation();
                AirConsoleLogger.Log(() => "Update download and installation initiated (confirmation already obtained).");
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
        /// Handles periodic checking logic with intelligent scheduling
        /// </summary>
        private static void PeriodicCheckHandler() {
            if (!_automaticCheckingInitialized) {
                return;
            }

            UpdateSettings settings = UpdateSettings.Instance;

            // Stop if automatic checking is disabled
            if (!settings.AutomaticCheckEnabled) {
                AirConsoleLogger.Log(() => "Automatic checking disabled, stopping periodic handler");
                StopAutomaticChecking();
                return;
            }

            // Disable automatic checking after persistent failures (5+ consecutive failures)
            if (settings.FailedCheckCount >= 5) {
                AirConsoleLogger.LogWarning(() =>
                    $"Disabling automatic checking after {settings.FailedCheckCount} consecutive failures. Use 'Reset Error State' to re-enable.");
                StopAutomaticChecking();
                return;
            }

            // Check for update status changes and handle notifications
            CheckForUpdateStatusChanges();

            // Skip if check is already in progress
            if (IsCheckInProgress) {
                return;
            }

            // Intelligent scheduling: Check if it's time for a scheduled check
            if (!_isScheduledCheckPending && ShouldPerformPeriodicCheck()) {
                _isScheduledCheckPending = true;

                // Schedule the check for the next editor update to avoid blocking
                EditorApplication.delayCall += () => {
                    _isScheduledCheckPending = false;

                    // Double-check conditions before actually performing the check
                    if (_automaticCheckingInitialized && settings.AutomaticCheckEnabled && ShouldPerformPeriodicCheck()) {
                        AirConsoleLogger.Log(() => "Performing scheduled periodic update check");
                        CheckForUpdatesAsync();
                    } else {
                        AirConsoleLogger.Log(() => "Skipping scheduled check due to changed conditions");
                    }
                };
            }
        }

        /// <summary>
        /// Determines if a periodic check should be performed based on intelligent scheduling
        /// </summary>
        /// <returns>True if a periodic check should be performed</returns>
        private static bool ShouldPerformPeriodicCheck() {
            UpdateSettings settings = UpdateSettings.Instance;

            // Basic rate limiting check
            if (!settings.CanCheckNow()) {
                return false;
            }

            // Don't check too frequently if we've had recent failures
            if (settings.FailedCheckCount > 0) {
                TimeSpan timeSinceLastError = DateTime.Now - settings.LastErrorTime;
                TimeSpan minimumWaitTime = TimeSpan.FromHours(Math.Min(24, Math.Pow(2, settings.FailedCheckCount)));

                if (timeSinceLastError < minimumWaitTime) {
                    return false;
                }
            }

            // Check if we're in a reasonable time window (avoid checking during likely inactive periods)
            DateTime now = DateTime.Now;
            int hourOfDay = now.Hour;

            // Prefer checking during typical working hours (8 AM to 8 PM) but don't be too restrictive
            // This is just a preference, not a hard requirement
            bool isPreferredTime = hourOfDay >= 8 && hourOfDay <= 20;

            // If it's been more than 48 hours since last check, check regardless of time
            TimeSpan timeSinceLastCheck = now - settings.LastCheckTime;
            bool isOverdue = timeSinceLastCheck.TotalHours > 48;

            return isPreferredTime || isOverdue;
        }

        /// <summary>
        /// Checks for changes in update availability and handles notifications
        /// </summary>
        private static void CheckForUpdateStatusChanges() {
            bool currentUpdateAvailable = IsUpdateAvailable;
            Version currentLatestVersion = LatestVersion;
            UpdateSettings settings = UpdateSettings.Instance;

            // Check if update status changed from false to true (new update detected)
            if (!_lastUpdateAvailableState && currentUpdateAvailable) {
                // Check if this is a new version we haven't notified about yet
                if (currentLatestVersion != null && (_lastNotifiedVersion == null || currentLatestVersion > _lastNotifiedVersion)) {
                    AirConsoleLogger.Log(() => $"New update detected: v{currentLatestVersion}.");

                    // Auto-open update checker window if enabled
                    if (settings.AutoOpenSettingsWindow) {
                        AirConsoleLogger.Log(() => "Auto-opening AirConsole Update Checker window to show update notification.");
                        EditorApplication.delayCall += () => { UpdateCheckerWindow.ShowWindow(); };
                    } else {
                        AirConsoleLogger.Log(() =>
                            "Auto-open update checker window is disabled. Update notification available in AirConsole Update Checker.");
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
                    UpdateSettings settings = UpdateSettings.Instance;

                    // Log startup initialization for debugging
                    AirConsoleLogger.Log(() =>
                        $"UpdateChecker initializing on editor startup. AutoCheck: {settings.AutomaticCheckEnabled}, CheckOnStartup: {settings.CheckOnStartup}");

                    // Start automatic checking system
                    StartAutomaticChecking();

                    // Register cleanup handler for editor shutdown
                    RegisterShutdownHandler();
                } catch (Exception ex) {
                    AirConsoleLogger.LogError(() => $"Failed to initialize UpdateChecker on startup: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// Registers the shutdown handler for proper cleanup
        /// </summary>
        private static void RegisterShutdownHandler() {
            // Register for editor shutdown to ensure proper cleanup
            EditorApplication.quitting += OnEditorShutdown;
            AirConsoleLogger.Log(() => "UpdateChecker shutdown handler registered");
        }

        /// <summary>
        /// Handles cleanup when the editor is shutting down
        /// </summary>
        private static void OnEditorShutdown() {
            try {
                AirConsoleLogger.Log(() => "UpdateChecker performing shutdown cleanup");

                // Stop automatic checking and clean up callbacks
                StopAutomaticChecking();

                // Unregister the shutdown handler to prevent multiple calls
                EditorApplication.quitting -= OnEditorShutdown;

                // Clear any pending delayed calls
                EditorApplication.delayCall -= () => CheckForUpdatesAsync();

                // Reset static state
                _automaticCheckingInitialized = false;
                _isScheduledCheckPending = false;
                _lastUpdateAvailableState = false;
                _lastNotifiedVersion = null;

                AirConsoleLogger.Log(() => "UpdateChecker shutdown cleanup completed");
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Error during UpdateChecker shutdown: {ex.Message}");
            }
        }
        #endregion
    }
}
#endif
