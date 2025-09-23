#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using System;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// ScriptableObject for persisting update checker settings and state
    /// </summary>
    [CreateAssetMenu(fileName = "UpdateSettings", menuName = "AirConsole/Update Settings")]
    public class UpdateSettings : ScriptableObject {
        private const string SettingsAssetPath = "Assets/AirConsole/resources/UpdateSettings.asset";

        
        [SerializeField]
        private bool automaticCheckEnabled = true;
        
        [SerializeField]
        private string lastCheckTime = ""; // DateTime serialized as string

        [SerializeField]
        private string dismissedVersion = "";
        
        [SerializeField]
        private int failedCheckCount = 0; // Track consecutive failures for backoff

        [SerializeField]
        private string lastErrorMessage = ""; // Last error message for debugging

        [SerializeField]
        private string lastErrorTime = ""; // When the last error occurred

        [SerializeField]
        private bool networkErrorDetected = false; // Flag for persistent network issues

        private static UpdateSettings _instance;

        /// <summary>
        /// Gets or sets whether automatic update checking is enabled
        /// </summary>
        public bool AutomaticCheckEnabled {
            get => automaticCheckEnabled;
            set {
                automaticCheckEnabled = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets the check interval in hours (fixed at 12 hours)
        /// </summary>
        public int CheckIntervalHours {
            get => 12; // Fixed 12-hour interval
        }

        /// <summary>
        /// Gets or sets the last check time
        /// </summary>
        public DateTime LastCheckTime {
            get => DateTime.TryParse(lastCheckTime, out DateTime dt) ? dt : DateTime.MinValue;
            set {
                lastCheckTime = value.ToString("O"); // ISO 8601 format
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the dismissed version string
        /// </summary>
        public string DismissedVersion {
            get => dismissedVersion ?? "";
            set {
                dismissedVersion = value ?? "";
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets whether to check on startup (always true)
        /// </summary>
        public bool CheckOnStartup {
            get => true; // Always check on startup
        }

        /// <summary>
        /// Gets whether to automatically open the update checker window when an update is available (always true)
        /// </summary>
        public bool AutoOpenSettingsWindow {
            get => true; // Always auto-open
        }

        /// <summary>
        /// Gets or sets the failed check count for exponential backoff
        /// </summary>
        public int FailedCheckCount {
            get => failedCheckCount;
            set {
                failedCheckCount = Mathf.Max(0, value);
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the last error message for debugging purposes
        /// </summary>
        public string LastErrorMessage {
            get => lastErrorMessage ?? "";
            set {
                lastErrorMessage = value ?? "";
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the time when the last error occurred
        /// </summary>
        public DateTime LastErrorTime {
            get => DateTime.TryParse(lastErrorTime, out DateTime dt) ? dt : DateTime.MinValue;
            set {
                lastErrorTime = value.ToString("O"); // ISO 8601 format
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets whether a persistent network error has been detected
        /// </summary>
        public bool NetworkErrorDetected {
            get => networkErrorDetected;
            set {
                networkErrorDetected = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Checks if an update check can be performed now based on rate limiting
        /// </summary>
        /// <returns>True if a check can be performed now</returns>
        public bool CanCheckNow() {
            if (!AutomaticCheckEnabled) {
                return false;
            }

            TimeSpan timeSinceLastCheck = DateTime.Now - LastCheckTime;
            TimeSpan requiredInterval = TimeSpan.FromHours(GetEffectiveCheckInterval());

            return timeSinceLastCheck >= requiredInterval;
        }

        /// <summary>
        /// Gets the time until the next check is allowed
        /// </summary>
        /// <returns>TimeSpan until next check, or TimeSpan.Zero if check is allowed now</returns>
        public TimeSpan TimeUntilNextCheck() {
            if (!AutomaticCheckEnabled) {
                return TimeSpan.MaxValue;
            }

            TimeSpan timeSinceLastCheck = DateTime.Now - LastCheckTime;
            TimeSpan requiredInterval = TimeSpan.FromHours(GetEffectiveCheckInterval());

            return timeSinceLastCheck >= requiredInterval ? TimeSpan.Zero : requiredInterval - timeSinceLastCheck;
        }

        /// <summary>
        /// Gets the effective check interval considering exponential backoff for failures
        /// </summary>
        /// <returns>Effective check interval in hours</returns>
        private int GetEffectiveCheckInterval() {
            if (FailedCheckCount == 0) {
                return CheckIntervalHours;
            }

            // Exponential backoff: 12h, 24h, 48h, 96h, max 96h
            int backoffMultiplier = Mathf.Min(1 << (FailedCheckCount - 1), 8);
            return CheckIntervalHours * backoffMultiplier;
        }

        /// <summary>
        /// Resets the failed check count and clears error state (call on successful check)
        /// </summary>
        public void ResetFailedCheckCount() {
            FailedCheckCount = 0;
            LastErrorMessage = "";
            LastErrorTime = DateTime.MinValue;
            NetworkErrorDetected = false;
        }

        /// <summary>
        /// Increments the failed check count and records error information
        /// </summary>
        /// <param name="errorMessage">The error message to record</param>
        /// <param name="isNetworkError">Whether this is a network-related error</param>
        public void IncrementFailedCheckCount(string errorMessage = null, bool isNetworkError = false) {
            FailedCheckCount++;

            if (!string.IsNullOrEmpty(errorMessage)) {
                LastErrorMessage = errorMessage;
                LastErrorTime = DateTime.Now;
            }

            if (isNetworkError) {
                NetworkErrorDetected = true;
            }
        }

        /// <summary>
        /// Increments the failed check count (backward compatibility overload)
        /// </summary>
        public void IncrementFailedCheckCount() {
            IncrementFailedCheckCount(null, false);
        }

        /// <summary>
        /// Gets diagnostic information about the current error state
        /// </summary>
        /// <returns>Formatted diagnostic string</returns>
        public string GetDiagnosticInfo() {
            if (FailedCheckCount == 0) {
                return "No recent errors";
            }

            string info = $"Failed checks: {FailedCheckCount}";

            if (LastErrorTime != DateTime.MinValue) {
                TimeSpan timeSinceError = DateTime.Now - LastErrorTime;
                info += $", Last error: {timeSinceError.TotalHours:F1}h ago";
            }

            if (!string.IsNullOrEmpty(LastErrorMessage)) {
                info += $", Message: {LastErrorMessage}";
            }

            if (NetworkErrorDetected) {
                info += " (Network issues detected)";
            }

            return info;
        }

        /// <summary>
        /// Checks if the system should attempt recovery from error state
        /// </summary>
        /// <returns>True if recovery should be attempted</returns>
        public bool ShouldAttemptRecovery() {
            // Attempt recovery if we've had network errors but some time has passed
            if (NetworkErrorDetected && FailedCheckCount > 0) {
                TimeSpan timeSinceError = DateTime.Now - LastErrorTime;
                return timeSinceError.TotalHours >= 24; // Try recovery after 24 hours
            }

            return false;
        }

        /// <summary>
        /// Marks the asset as dirty for saving
        /// </summary>
        private void MarkDirty() {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Gets the singleton instance of UpdateSettings, creating it if necessary
        /// </summary>
        /// <returns>The UpdateSettings instance</returns>
        public static UpdateSettings Instance {
            get {
                if (!_instance) {
                    _instance = LoadOrCreateSettings();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Loads existing settings or creates new ones with default values
        /// </summary>
        /// <returns>UpdateSettings instance</returns>
        private static UpdateSettings LoadOrCreateSettings() {
#if UNITY_EDITOR

            // Try to load existing asset
            UpdateSettings settings = AssetDatabase.LoadAssetAtPath<UpdateSettings>(SettingsAssetPath);

            if (!settings) {
                // Create new settings with default values
                settings = CreateInstance<UpdateSettings>();

                // Ensure the directory exists
                string directory = System.IO.Path.GetDirectoryName(SettingsAssetPath);
                if (!System.IO.Directory.Exists(directory)) {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // Create the asset
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return settings;
#else
            // Fallback for runtime (shouldn't be used in editor-only code)
            return CreateInstance<UpdateSettings>();
#endif
        }

        /// <summary>
        /// Validates settings on load
        /// </summary>
        private void OnEnable() {
            // Ensure fixed settings are applied
            automaticCheckEnabled = true;
            failedCheckCount = 0;
            MarkDirty();
        }
    }
}
#endif
