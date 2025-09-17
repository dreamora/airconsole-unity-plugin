#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Networking;

    internal static class GithubUpdate {
        private const string GithubLatestAPI = "https://api.github.com/repos/airconsole/airconsole-unity-plugin/releases/latest";

        private static bool _updateCheckStarted;
        private static bool _updateCheckInProgress;
        private static bool _updateAvailable;
        private static Version _latestVersion;
        private static GithubRelease _cachedLatestRelease;
        private static UnityWebRequest _checkRequest;
        private static UnityWebRequestAsyncOperation _checkOp;
        private static DateTime _lastCheckTime;

        // Public properties to expose update status
        public static bool IsUpdateAvailable => _updateAvailable;
        public static Version LatestVersion => _latestVersion;
        public static bool IsCheckInProgress => _updateCheckInProgress;
        public static DateTime LastCheckTime => _lastCheckTime;

        // Internal properties for backward compatibility
        internal static bool UpdateCheckInProgress => _updateCheckInProgress;
        internal static bool UpdateAvailable => _updateAvailable;

        /// <summary>
        /// Begins a background update check with rate limiting validation
        /// </summary>
        /// <param name="forceCheck">If true, bypasses rate limiting (for manual checks)</param>
        /// <returns>True if check was started, false if rate limited or already in progress</returns>
        public static bool BeginBackgroundUpdateCheck(bool forceCheck = false) {
            if (_updateCheckInProgress) {
                return false;
            }

            var settings = UpdateSettings.Instance;

            // Validate rate limiting unless forced
            if (!forceCheck && !settings.CanCheckNow()) {
                AirConsoleLogger.Log(() => $"Update check rate limited. Next check available in {settings.TimeUntilNextCheck():hh\\:mm\\:ss}");
                return false;
            }

            _updateCheckStarted = true;
            _updateCheckInProgress = true;
            _lastCheckTime = DateTime.Now;

            try {
                _checkRequest = UnityWebRequest.Get(GithubLatestAPI);
                _checkRequest.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                _checkRequest.timeout = 30; // 30 second timeout
                _checkOp = _checkRequest.SendWebRequest();
                EditorApplication.update += PollUpdateCheck;

                // Update last check time in settings
                settings.LastCheckTime = _lastCheckTime;

                return true;
            } catch (Exception e) {
                _updateCheckInProgress = false;
                HandleCheckFailure(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        internal static void BeginBackgroundUpdateCheck() {
            BeginBackgroundUpdateCheck(false);
        }

        private static void PollUpdateCheck() {
            if (_checkOp == null) {
                return;
            }

            bool done = _checkOp.isDone && _checkRequest.result != UnityWebRequest.Result.InProgress;
            if (!done) {
                return;
            }

            var settings = UpdateSettings.Instance;
            bool success = false;

            try {
                if (_checkRequest.result == UnityWebRequest.Result.Success) {
                    string json = _checkRequest.downloadHandler.text;
                    GithubRelease release = JsonUtility.FromJson<GithubRelease>(json);
                    _cachedLatestRelease = release;

                    if (TryParseVersionFromTag(release?.tag_name ?? release?.name, out var latest) &&
                        Version.TryParse(Settings.VERSION, out var current)) {
                        _latestVersion = latest;
                        _updateAvailable = latest > current;
                        success = true;

                        // Reset failed check count on successful check
                        settings.ResetFailedCheckCount();

                        AirConsoleLogger.Log(() => $"Update check completed. Current: v{current}, Latest: v{latest}, Update available: {_updateAvailable}");
                    } else {
                        _updateAvailable = false;
                        HandleCheckFailure("Failed to parse version information");
                    }
                } else {
                    // Handle specific error cases
                    string errorMessage = $"Update check failed: {_checkRequest.responseCode} {_checkRequest.error}";

                    // Check for rate limiting (HTTP 403 with rate limit headers)
                    if (_checkRequest.responseCode == 403) {
                        var headers = _checkRequest.GetResponseHeaders();
                        if (headers != null && (headers.ContainsKey("X-RateLimit-Remaining") || headers.ContainsKey("x-ratelimit-remaining"))) {
                            errorMessage = "GitHub API rate limit exceeded. Update checks will be delayed.";
                        }
                    }

                    HandleCheckFailure(errorMessage);
                    _updateAvailable = false;
                }
            } catch (Exception ex) {
                HandleCheckFailure($"Update check error: {ex.Message}");
                _updateAvailable = false;
            } finally {
                _updateCheckInProgress = false;

                // Clean up request resources
                if (_checkRequest != null) {
                    _checkRequest.Dispose();
                    _checkRequest = null;
                }
                _checkOp = null;
                EditorApplication.update -= PollUpdateCheck;

                // Repaint UI to reflect changes
                InternalEditorUtility.RepaintAllViews();
            }
        }

        /// <summary>
        /// Handles check failures with exponential backoff logic
        /// </summary>
        /// <param name="errorMessage">The error message to log</param>
        private static void HandleCheckFailure(string errorMessage) {
            var settings = UpdateSettings.Instance;
            settings.IncrementFailedCheckCount();

            // Log appropriate level based on failure count
            if (settings.FailedCheckCount <= 2) {
                AirConsoleLogger.LogWarning(() => errorMessage);
            } else {
                AirConsoleLogger.LogError(() => $"{errorMessage} (Failure #{settings.FailedCheckCount})");
            }

            // Disable automatic checking after 5 consecutive failures
            if (settings.FailedCheckCount >= 5) {
                settings.AutomaticCheckEnabled = false;
                AirConsoleLogger.LogError(() => "Automatic update checking disabled after 5 consecutive failures. Re-enable in AirConsole settings.");
            }
        }

        /// <summary>
        /// Checks if an update check can be performed now based on rate limiting
        /// </summary>
        /// <returns>True if a check can be performed now</returns>
        public static bool CanCheckNow() {
            return UpdateSettings.Instance.CanCheckNow();
        }

        /// <summary>
        /// Gets the time until the next check is allowed
        /// </summary>
        /// <returns>TimeSpan until next check, or TimeSpan.Zero if check is allowed now</returns>
        public static TimeSpan TimeUntilNextCheck() {
            return UpdateSettings.Instance.TimeUntilNextCheck();
        }

        /// <summary>
        /// Gets the current version of the plugin
        /// </summary>
        /// <returns>Current plugin version</returns>
        public static Version GetCurrentVersion() {
            return Version.TryParse(Settings.VERSION, out var version) ? version : new Version(0, 0, 0);
        }

        /// <summary>
        /// Checks if the specified version is dismissed by the user
        /// </summary>
        /// <param name="version">Version to check</param>
        /// <returns>True if the version is dismissed</returns>
        public static bool IsVersionDismissed(Version version) {
            if (version == null) return false;
            var dismissedVersion = UpdateSettings.Instance.DismissedVersion;
            return !string.IsNullOrEmpty(dismissedVersion) && dismissedVersion == version.ToString();
        }

        /// <summary>
        /// Dismisses the specified version so it won't show update notifications
        /// </summary>
        /// <param name="version">Version to dismiss</param>
        public static void DismissVersion(Version version) {
            if (version != null) {
                UpdateSettings.Instance.DismissedVersion = version.ToString();
            }
        }

        private static bool TryParseVersionFromTag(string tag, out Version version) {
            version = null;
            if (string.IsNullOrEmpty(tag)) {
                return false;
            }
            var versionStr = tag.Trim();
            if (versionStr.StartsWith("v", StringComparison.OrdinalIgnoreCase)) {
                versionStr = versionStr.Substring(1);
            }
            return Version.TryParse(versionStr, out version);
        }

        [Serializable]
        private class GithubAsset {
            public string name;
            public string browser_download_url;
        }

        [Serializable]
        private class GithubRelease {
            public string tag_name;
            public string name;
            public GithubAsset[] assets;
            public string html_url;
        }

        internal static void TryUpdatePluginFromGithub() {
            try {
                EditorUtility.DisplayProgressBar("AirConsole", "Checking latest release…", 0.1f);
                GithubRelease release = GetLatestRelease();
                if (release == null) {
                    EditorUtility.DisplayDialog("AirConsole", "Failed to retrieve release info.", "OK");
                    return;
                }

                if (!ConfirmUpdate(release)) {
                    return;
                }

                GithubAsset package = FindDownloadableAsset(release);
                if (package == null) {
                    HandleNoDownloadableAsset(release);
                    return;
                }

                DownloadAndImportPackage(package);

            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Update failed: {ex.Message}");
                EditorUtility.DisplayDialog("AirConsole", "Update failed. See console for details.", "OK");
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        private static GithubRelease GetLatestRelease() {
            if (_cachedLatestRelease != null) {
                return _cachedLatestRelease;
            }

            string json = DownloadString(GithubLatestAPI, out _);
            if (string.IsNullOrEmpty(json)) {
                return null;
            }

            return JsonUtility.FromJson<GithubRelease>(json);
        }

        private static bool ConfirmUpdate(GithubRelease release) {
            string latestTag = (release.tag_name ?? release.name ?? "").Trim();

            bool latestOk = TryParseVersionFromTag(latestTag, out var latest);
            bool currentOk = Version.TryParse(Settings.VERSION, out var current);

            if (currentOk && latestOk && latest <= current) {
                EditorUtility.DisplayDialog("AirConsole", $"Plugin is up to date (v{Settings.VERSION}).", "OK");
                return false;
            }

            string msg = latestOk
                ? $"Update available: v{Settings.VERSION} → v{latest}"
                : $"A newer release may be available: '{latestTag}'.\nCurrent: v{Settings.VERSION}";

            return EditorUtility.DisplayDialog("AirConsole", msg + "\n\nDownload and import the latest release?", "Update", "Cancel");
        }

        private static GithubAsset FindDownloadableAsset(GithubRelease release) {
            return release.assets?.FirstOrDefault(a => a?.name?.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase) == true)
                   ?? release.assets?.FirstOrDefault(a => a?.name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static void HandleNoDownloadableAsset(GithubRelease release) {
            if (!string.IsNullOrEmpty(release.html_url)) {
                bool open = EditorUtility.DisplayDialog("AirConsole", "Could not find a .unitypackage asset in the latest release.",
                    "Open Releases", "Close");
                if (open) {
                    Application.OpenURL(release.html_url);
                }
            } else {
                EditorUtility.DisplayDialog("AirConsole", "Could not find downloadable assets for the latest release.", "OK");
            }
        }

        private static void DownloadAndImportPackage(GithubAsset package) {
            string tempPath = Path.Combine(Path.GetTempPath(),
                package.name ?? "airconsole-plugin-" + Guid.NewGuid().ToString("N") + ".unitypackage");

            EditorUtility.DisplayProgressBar("AirConsole", "Downloading package…", 0.4f);
            byte[] data = DownloadBytes(package.browser_download_url, out _);

            if (data == null || data.Length == 0) {
                EditorUtility.DisplayDialog("AirConsole", "Failed to download the package.", "OK");
                return;
            }

            File.WriteAllBytes(tempPath, data);
            EditorUtility.DisplayProgressBar("AirConsole", "Importing package…", 0.9f);

            if (tempPath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)) {
                AssetDatabase.ImportPackage(tempPath, true);
            } else {
                bool open = EditorUtility.DisplayDialog("AirConsole",
                    "Downloaded asset is not a .unitypackage. Open in browser instead?", "Open", "Cancel");
                if (open) {
                    Application.OpenURL(package.browser_download_url);
                }
            }
        }

        private static T Download<T>(string url, string progressMessage, Func<DownloadHandler, T> getResult, out long contentLength) where T : class {
            contentLength = -1;
            using (var req = UnityWebRequest.Get(url)) {
                req.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                var op = req.SendWebRequest();
                while (!op.isDone) {
                    EditorUtility.DisplayProgressBar("AirConsole", progressMessage, req.downloadProgress);
                }

                if (req.result != UnityWebRequest.Result.Success) {
                    AirConsoleLogger.LogError(() => $"HTTP error: {req.responseCode} {req.error}");
                    return null;
                }

                if (req.GetResponseHeaders()?.ContainsKey("CONTENT-LENGTH") == true) {
                    long.TryParse(req.GetResponseHeader("CONTENT-LENGTH"), out contentLength);
                } else {
                    contentLength = -1;
                }
                return getResult(req.downloadHandler);
            }
        }

        private static string DownloadString(string url, out long contentLength) {
            return Download(url, "Contacting GitHub…", handler => handler.text, out contentLength);
        }

        private static byte[] DownloadBytes(string url, out long contentLength) {
            return Download(url, "Downloading…", handler => handler.data, out contentLength);
        }
    }
}
#endif