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
        private const int MINIMUM_CHECK_INTERVAL_HOURS = 24;
        private const string SETTINGS_ASSET_PATH = "Assets/AirConsole/resources/UpdateSettings.asset";

        [SerializeField] private bool automaticCheckEnabled = true;
        [SerializeField] private int checkIntervalHours = 24; // Minimum 24 hours to prevent rate limiting
        [SerializeField] private string lastCheckTime = ""; // DateTime serialized as string
        [SerializeField] private string dismissedVersion = "";
        [SerializeField] private bool checkOnStartup = true;
        [SerializeField] private int failedCheckCount = 0; // Track consecutive failures for backoff

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
        /// Gets or sets the check interval in hours (minimum 24 hours)
        /// </summary>
        public int CheckIntervalHours {
            get => Mathf.Max(MINIMUM_CHECK_INTERVAL_HOURS, checkIntervalHours);
            set {
                checkIntervalHours = Mathf.Max(MINIMUM_CHECK_INTERVAL_HOURS, value);
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets or sets the last check time
        /// </summary>
        public DateTime LastCheckTime {
            get => DateTime.TryParse(lastCheckTime, out var dt) ? dt : DateTime.MinValue;
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
        /// Gets or sets whether to check on startup
        /// </summary>
        public bool CheckOnStartup {
            get => checkOnStartup;
            set {
                checkOnStartup = value;
                MarkDirty();
            }
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
        /// Checks if an update check can be performed now based on rate limiting
        /// </summary>
        /// <returns>True if a check can be performed now</returns>
        public bool CanCheckNow() {
            if (!AutomaticCheckEnabled) {
                return false;
            }

            var timeSinceLastCheck = DateTime.Now - LastCheckTime;
            var requiredInterval = TimeSpan.FromHours(GetEffectiveCheckInterval());

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

            var timeSinceLastCheck = DateTime.Now - LastCheckTime;
            var requiredInterval = TimeSpan.FromHours(GetEffectiveCheckInterval());

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

            // Exponential backoff: 24h, 48h, 96h, 192h, max 192h
            int backoffMultiplier = Mathf.Min(1 << (FailedCheckCount - 1), 8);
            return CheckIntervalHours * backoffMultiplier;
        }

        /// <summary>
        /// Resets the failed check count (call on successful check)
        /// </summary>
        public void ResetFailedCheckCount() {
            FailedCheckCount = 0;
        }

        /// <summary>
        /// Increments the failed check count (call on failed check)
        /// </summary>
        public void IncrementFailedCheckCount() {
            FailedCheckCount++;
        }

        /// <summary>
        /// Marks the asset as dirty for saving
        /// </summary>
        private void MarkDirty() {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Gets the singleton instance of UpdateSettings, creating it if necessary
        /// </summary>
        /// <returns>The UpdateSettings instance</returns>
        public static UpdateSettings Instance {
            get {
                if (_instance == null) {
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
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<UpdateSettings>(SETTINGS_ASSET_PATH);

            if (settings == null) {
                // Create new settings with default values
                settings = CreateInstance<UpdateSettings>();

                // Ensure the directory exists
                var directory = System.IO.Path.GetDirectoryName(SETTINGS_ASSET_PATH);
                if (!System.IO.Directory.Exists(directory)) {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // Create the asset
                UnityEditor.AssetDatabase.CreateAsset(settings, SETTINGS_ASSET_PATH);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
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
            // Ensure minimum interval is respected
            if (checkIntervalHours < MINIMUM_CHECK_INTERVAL_HOURS) {
                checkIntervalHours = MINIMUM_CHECK_INTERVAL_HOURS;
                MarkDirty();
            }
        }
    }
}
#endif