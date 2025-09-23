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
                AirConsoleLogger.Log(() => "Update check already in progress, skipping new request");
                return false;
            }

            var settings = UpdateSettings.Instance;

            // Validate rate limiting unless forced
            if (!forceCheck && !settings.CanCheckNow()) {
                var timeUntilNext = settings.TimeUntilNextCheck();
                AirConsoleLogger.Log(() => $"Update check rate limited. Next check available in {timeUntilNext:hh\\:mm\\:ss}");
                return false;
            }

            _updateCheckStarted = true;
            _updateCheckInProgress = true;
            _lastCheckTime = DateTime.Now;

            try {
                AirConsoleLogger.Log(() => $"Starting update check. Force: {forceCheck}, Failed count: {settings.FailedCheckCount}");

                _checkRequest = UnityWebRequest.Get(GithubLatestAPI);
                _checkRequest.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater/1.0");
                _checkRequest.timeout = 30; // 30 second timeout

                // Add additional headers for better compatibility
                _checkRequest.SetRequestHeader("Accept", "application/vnd.github.v3+json");
                _checkRequest.SetRequestHeader("Cache-Control", "no-cache");

                _checkOp = _checkRequest.SendWebRequest();
                EditorApplication.update += PollUpdateCheck;

                // Update last check time in settings
                settings.LastCheckTime = _lastCheckTime;

                AirConsoleLogger.Log(() => "Update check request sent successfully");
                return true;
            } catch (Exception e) {
                _updateCheckInProgress = false;
                HandleCheckFailure($"Failed to initiate update check: {e.Message}", e);
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
                AirConsoleLogger.LogWarning(() => "PollUpdateCheck called but _checkOp is null");
                return;
            }

            bool done = _checkOp.isDone && _checkRequest.result != UnityWebRequest.Result.InProgress;
            if (!done) {
                return;
            }

            var settings = UpdateSettings.Instance;
            bool success = false;
            string debugInfo = "";

            try {
                // Log detailed request information for debugging
                debugInfo = $"Response Code: {_checkRequest.responseCode}, " +
                           $"Result: {_checkRequest.result}, " +
                           $"Error: {_checkRequest.error ?? "None"}, " +
                           $"Downloaded Bytes: {_checkRequest.downloadedBytes}";

                AirConsoleLogger.Log(() => $"Update check completed. {debugInfo}");

                if (_checkRequest.result == UnityWebRequest.Result.Success) {
                    success = ProcessSuccessfulResponse(settings);
                } else {
                    ProcessFailedResponse(settings, debugInfo);
                    _updateAvailable = false;
                }
            } catch (Exception ex) {
                HandleCheckFailure($"Update check processing error: {ex.Message}", ex, debugInfo);
                _updateAvailable = false;
            } finally {
                _updateCheckInProgress = false;

                // Clean up request resources
                CleanupWebRequest();

                // Repaint UI to reflect changes
                InternalEditorUtility.RepaintAllViews();

                AirConsoleLogger.Log(() => $"Update check cleanup completed. Success: {success}");
            }
        }

        /// <summary>
        /// Processes a successful HTTP response from GitHub API
        /// </summary>
        /// <param name="settings">Update settings instance</param>
        /// <returns>True if processing was successful</returns>
        private static bool ProcessSuccessfulResponse(UpdateSettings settings) {
            try {
                string json = _checkRequest.downloadHandler.text;

                if (string.IsNullOrEmpty(json)) {
                    HandleCheckFailure("Received empty response from GitHub API", null, "Response was successful but contained no data");
                    return false;
                }

                AirConsoleLogger.Log(() => $"Received GitHub API response: {json.Length} characters");

                GithubRelease release = JsonUtility.FromJson<GithubRelease>(json);
                if (release == null) {
                    HandleCheckFailure("Failed to parse GitHub API response as JSON", null, $"JSON content: {json.Substring(0, Math.Min(200, json.Length))}...");
                    return false;
                }

                _cachedLatestRelease = release;

                if (TryParseVersionFromTag(release?.tag_name ?? release?.name, out var latest) &&
                    Version.TryParse(Settings.VERSION, out var current)) {

                    _latestVersion = latest;
                    _updateAvailable = latest > current;

                    // Reset failed check count on successful check
                    settings.ResetFailedCheckCount();

                    AirConsoleLogger.Log(() => $"Update check completed successfully. Current: v{current}, Latest: v{latest}, Update available: {_updateAvailable}");
                    return true;
                } else {
                    string versionInfo = $"Tag: '{release?.tag_name}', Name: '{release?.name}', Current: '{Settings.VERSION}'";
                    HandleCheckFailure("Failed to parse version information from GitHub response", null, versionInfo);
                    return false;
                }
            } catch (Exception ex) {
                HandleCheckFailure($"Error processing successful GitHub API response: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Processes a failed HTTP response from GitHub API
        /// </summary>
        /// <param name="settings">Update settings instance</param>
        /// <param name="debugInfo">Additional debug information</param>
        private static void ProcessFailedResponse(UpdateSettings settings, string debugInfo) {
            string errorMessage = BuildErrorMessage(_checkRequest.responseCode, _checkRequest.error);

            // Handle specific error scenarios
            switch (_checkRequest.result) {
                case UnityWebRequest.Result.ConnectionError:
                    HandleNetworkError(errorMessage, debugInfo);
                    break;

                case UnityWebRequest.Result.ProtocolError:
                    HandleProtocolError(_checkRequest.responseCode, errorMessage, debugInfo);
                    break;

                case UnityWebRequest.Result.DataProcessingError:
                    HandleDataProcessingError(errorMessage, debugInfo);
                    break;

                default:
                    HandleCheckFailure(errorMessage, null, debugInfo);
                    break;
            }
        }

        /// <summary>
        /// Handles network connection errors (DNS, timeout, proxy issues)
        /// </summary>
        /// <param name="errorMessage">Base error message</param>
        /// <param name="debugInfo">Additional debug information</param>
        private static void HandleNetworkError(string errorMessage, string debugInfo) {
            string enhancedMessage = "Network connection failed. ";

            if (errorMessage.Contains("timeout") || errorMessage.Contains("timed out")) {
                enhancedMessage += "Request timed out - this may indicate slow network or proxy issues.";
            } else if (errorMessage.Contains("DNS") || errorMessage.Contains("resolve")) {
                enhancedMessage += "DNS resolution failed - check internet connection and DNS settings.";
            } else if (errorMessage.Contains("proxy") || errorMessage.Contains("407")) {
                enhancedMessage += "Proxy authentication may be required. Check proxy settings in Unity or system.";
            } else {
                enhancedMessage += "Check internet connection and firewall settings.";
            }

            enhancedMessage += $" Original error: {errorMessage}";

            HandleCheckFailure(enhancedMessage, null, debugInfo);
        }

        /// <summary>
        /// Handles HTTP protocol errors (4xx, 5xx status codes)
        /// </summary>
        /// <param name="responseCode">HTTP response code</param>
        /// <param name="errorMessage">Base error message</param>
        /// <param name="debugInfo">Additional debug information</param>
        private static void HandleProtocolError(long responseCode, string errorMessage, string debugInfo) {
            string enhancedMessage;

            switch (responseCode) {
                case 403:
                    enhancedMessage = HandleRateLimitError();
                    break;

                case 404:
                    enhancedMessage = "GitHub repository or API endpoint not found. The plugin may need to be updated.";
                    break;

                case 500:
                case 502:
                case 503:
                case 504:
                    enhancedMessage = "GitHub API is temporarily unavailable. Update checks will retry automatically.";
                    break;

                default:
                    enhancedMessage = $"GitHub API returned HTTP {responseCode}. {errorMessage}";
                    break;
            }

            HandleCheckFailure(enhancedMessage, null, debugInfo);
        }

        /// <summary>
        /// Handles GitHub API rate limiting with detailed information
        /// </summary>
        /// <returns>Enhanced error message for rate limiting</returns>
        private static string HandleRateLimitError() {
            var headers = _checkRequest.GetResponseHeaders();
            string rateLimitMessage = "GitHub API rate limit exceeded.";

            if (headers != null) {
                // Check for rate limit headers (GitHub uses both formats)
                string remaining = GetHeaderValue(headers, "X-RateLimit-Remaining", "x-ratelimit-remaining");
                string resetTime = GetHeaderValue(headers, "X-RateLimit-Reset", "x-ratelimit-reset");
                string limit = GetHeaderValue(headers, "X-RateLimit-Limit", "x-ratelimit-limit");

                if (!string.IsNullOrEmpty(remaining) || !string.IsNullOrEmpty(resetTime)) {
                    rateLimitMessage += $" Remaining: {remaining ?? "unknown"}, Limit: {limit ?? "unknown"}";

                    if (!string.IsNullOrEmpty(resetTime) && long.TryParse(resetTime, out var resetUnixTime)) {
                        var resetDateTime = DateTimeOffset.FromUnixTimeSeconds(resetUnixTime).ToLocalTime();
                        rateLimitMessage += $", Resets at: {resetDateTime:yyyy-MM-dd HH:mm:ss}";
                    }
                }
            }

            rateLimitMessage += " Update checks will be delayed with exponential backoff.";
            return rateLimitMessage;
        }

        /// <summary>
        /// Gets header value with case-insensitive fallback
        /// </summary>
        /// <param name="headers">Response headers dictionary</param>
        /// <param name="primaryKey">Primary header key</param>
        /// <param name="fallbackKey">Fallback header key</param>
        /// <returns>Header value or null if not found</returns>
        private static string GetHeaderValue(System.Collections.Generic.Dictionary<string, string> headers, string primaryKey, string fallbackKey) {
            if (headers.TryGetValue(primaryKey, out var value)) {
                return value;
            }
            if (headers.TryGetValue(fallbackKey, out value)) {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Handles data processing errors
        /// </summary>
        /// <param name="errorMessage">Base error message</param>
        /// <param name="debugInfo">Additional debug information</param>
        private static void HandleDataProcessingError(string errorMessage, string debugInfo) {
            string enhancedMessage = "Failed to process response data from GitHub API. " +
                                   "This may indicate corrupted data or an unexpected response format. " +
                                   $"Original error: {errorMessage}";

            HandleCheckFailure(enhancedMessage, null, debugInfo);
        }

        /// <summary>
        /// Builds a comprehensive error message from response code and error
        /// </summary>
        /// <param name="responseCode">HTTP response code</param>
        /// <param name="error">Error message from UnityWebRequest</param>
        /// <returns>Formatted error message</returns>
        private static string BuildErrorMessage(long responseCode, string error) {
            if (responseCode > 0) {
                return $"HTTP {responseCode}: {error ?? "Unknown error"}";
            } else {
                return error ?? "Unknown network error";
            }
        }

        /// <summary>
        /// Cleans up web request resources
        /// </summary>
        private static void CleanupWebRequest() {
            try {
                if (_checkRequest != null) {
                    _checkRequest.Dispose();
                    _checkRequest = null;
                }
                _checkOp = null;
                EditorApplication.update -= PollUpdateCheck;
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Error during web request cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles check failures with exponential backoff logic and comprehensive logging
        /// </summary>
        /// <param name="errorMessage">The error message to log</param>
        /// <param name="exception">Optional exception that caused the failure</param>
        /// <param name="debugInfo">Optional additional debug information</param>
        private static void HandleCheckFailure(string errorMessage, Exception exception = null, string debugInfo = null) {
            var settings = UpdateSettings.Instance;

            // Determine if this is a network-related error
            bool isNetworkError = IsNetworkRelatedError(errorMessage, exception);

            // Record the failure with enhanced tracking
            settings.IncrementFailedCheckCount(errorMessage, isNetworkError);

            // Build comprehensive error message
            string fullMessage = $"Update check failed: {errorMessage}";

            if (!string.IsNullOrEmpty(debugInfo)) {
                fullMessage += $" Debug info: {debugInfo}";
            }

            if (exception != null) {
                fullMessage += $" Exception: {exception.GetType().Name} - {exception.Message}";

                // Log stack trace for development builds
                AirConsoleLogger.LogDevelopment(() => $"Update check exception stack trace: {exception.StackTrace}");
            }

            // Calculate next check time with exponential backoff
            var nextCheckTime = DateTime.Now.Add(settings.TimeUntilNextCheck());
            fullMessage += $" (Failure #{settings.FailedCheckCount}, next check: {nextCheckTime:yyyy-MM-dd HH:mm:ss})";

            // Add diagnostic information
            string diagnosticInfo = settings.GetDiagnosticInfo();
            if (!string.IsNullOrEmpty(diagnosticInfo)) {
                fullMessage += $" Diagnostics: {diagnosticInfo}";
            }

            // Log appropriate level based on failure count and severity
            if (settings.FailedCheckCount == 1) {
                AirConsoleLogger.LogWarning(() => fullMessage);
            } else if (settings.FailedCheckCount <= 3) {
                AirConsoleLogger.LogWarning(() => fullMessage);
            } else {
                AirConsoleLogger.LogError(() => fullMessage);
            }

            // Log exception separately for better visibility
            if (exception != null) {
                AirConsoleLogger.LogException(exception);
            }

            // Disable automatic checking after 5 consecutive failures
            if (settings.FailedCheckCount >= 5) {
                settings.AutomaticCheckEnabled = false;
                string disableMessage = "Automatic update checking disabled after 5 consecutive failures. " +
                                      "This may indicate persistent network issues, proxy configuration problems, " +
                                      "or GitHub API access restrictions. Check your network settings and " +
                                      "re-enable in AirConsole settings when resolved.";

                AirConsoleLogger.LogError(() => disableMessage);

                // Show a user-friendly dialog for critical failures (only once per session)
                ShowCriticalErrorDialog(settings);
            }

            // Log troubleshooting information for network-related failures
            LogTroubleshootingInfo(errorMessage, settings.FailedCheckCount, isNetworkError);
        }

        /// <summary>
        /// Logs troubleshooting information based on the type of failure
        /// </summary>
        /// <param name="errorMessage">The original error message</param>
        /// <param name="failureCount">Number of consecutive failures</param>
        /// <param name="isNetworkError">Whether this is a network-related error</param>
        private static void LogTroubleshootingInfo(string errorMessage, int failureCount, bool isNetworkError) {
            // Only log troubleshooting info on first few failures to avoid spam
            if (failureCount > 3) return;

            string troubleshootingInfo = "Update check troubleshooting: ";

            if (errorMessage.Contains("timeout") || errorMessage.Contains("timed out")) {
                troubleshootingInfo += "Timeout issues may be caused by slow network, proxy servers, or firewall restrictions. " +
                                     "Try checking your network connection and proxy settings.";
            } else if (errorMessage.Contains("DNS") || errorMessage.Contains("resolve")) {
                troubleshootingInfo += "DNS resolution issues may indicate network connectivity problems or DNS server issues. " +
                                     "Try using a different DNS server (e.g., 8.8.8.8) or check your network settings.";
            } else if (errorMessage.Contains("403") || errorMessage.Contains("rate limit")) {
                troubleshootingInfo += "Rate limiting is normal and will resolve automatically. " +
                                     "The system uses exponential backoff to respect GitHub's API limits.";
            } else if (errorMessage.Contains("proxy") || errorMessage.Contains("407")) {
                troubleshootingInfo += "Proxy authentication issues detected. Check your proxy settings in Unity preferences " +
                                     "or system network settings. Corporate networks may require additional configuration.";
            } else if (errorMessage.Contains("SSL") || errorMessage.Contains("certificate")) {
                troubleshootingInfo += "SSL/Certificate issues may be caused by corporate firewalls or outdated certificates. " +
                                     "Contact your IT administrator if you're on a corporate network.";
            } else if (isNetworkError) {
                troubleshootingInfo += "Network connectivity issues detected. Check your internet connection, " +
                                     "firewall settings, proxy configuration, and ensure GitHub.com is accessible from your network.";
            } else {
                troubleshootingInfo += "General update check issues. This may be caused by GitHub API changes, " +
                                     "response format changes, or temporary service issues.";
            }

            // Add additional context for corporate environments
            if (isNetworkError && failureCount >= 2) {
                troubleshootingInfo += " If you're on a corporate network, contact your IT administrator " +
                                     "about GitHub API access and proxy configuration.";
            }

            AirConsoleLogger.Log(() => troubleshootingInfo);
        }

        /// <summary>
        /// Determines if an error is network-related
        /// </summary>
        /// <param name="errorMessage">Error message to analyze</param>
        /// <param name="exception">Exception to analyze</param>
        /// <returns>True if the error appears to be network-related</returns>
        private static bool IsNetworkRelatedError(string errorMessage, Exception exception) {
            if (string.IsNullOrEmpty(errorMessage)) return false;

            string lowerMessage = errorMessage.ToLower();

            // Check for common network error indicators
            return lowerMessage.Contains("timeout") ||
                   lowerMessage.Contains("connection") ||
                   lowerMessage.Contains("network") ||
                   lowerMessage.Contains("dns") ||
                   lowerMessage.Contains("proxy") ||
                   lowerMessage.Contains("firewall") ||
                   lowerMessage.Contains("ssl") ||
                   lowerMessage.Contains("certificate") ||
                   lowerMessage.Contains("resolve") ||
                   lowerMessage.Contains("unreachable") ||
                   lowerMessage.Contains("refused") ||
                   (exception != null && (
                       exception is System.Net.WebException ||
                       exception is System.Net.Sockets.SocketException ||
                       exception is System.TimeoutException
                   ));
        }

        /// <summary>
        /// Shows a critical error dialog to the user (with session-based throttling)
        /// </summary>
        /// <param name="settings">Update settings instance</param>
        private static void ShowCriticalErrorDialog(UpdateSettings settings) {
            // Use a static flag to prevent multiple dialogs per session
            if (_criticalErrorDialogShown) return;
            _criticalErrorDialogShown = true;

            string dialogMessage = "Update checking has been disabled due to repeated failures.\n\n";

            if (settings.NetworkErrorDetected) {
                dialogMessage += "Network connectivity issues detected. This may be caused by:\n" +
                               "• Firewall or proxy restrictions\n" +
                               "• Corporate network policies\n" +
                               "• Internet connectivity problems\n\n";
            } else {
                dialogMessage += "This may be caused by GitHub API issues or configuration problems.\n\n";
            }

            dialogMessage += "You can re-enable update checking in the AirConsole settings once the issue is resolved.";

            if (UnityEditor.EditorUtility.DisplayDialog(
                "AirConsole Update Checker",
                dialogMessage,
                "OK", "Open Settings")) {

                // User chose to open settings
                UnityEditor.EditorApplication.delayCall += () => {
                    SettingWindow.OpenSettingsWindow();
                };
            }
        }

        // Static flag to prevent multiple critical error dialogs per session
        private static bool _criticalErrorDialogShown = false;

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

        /// <summary>
        /// Updates the plugin from GitHub without showing the confirmation dialog
        /// Used when confirmation has already been obtained from the UI
        /// </summary>
        internal static void UpdatePluginFromGithubWithoutConfirmation() {
            try {
                EditorUtility.DisplayProgressBar("AirConsole", "Checking latest release…", 0.1f);
                GithubRelease release = GetLatestRelease();
                if (release == null) {
                    EditorUtility.DisplayDialog("AirConsole", "Failed to retrieve release info.", "OK");
                    return;
                }

                // Skip the ConfirmUpdate step since confirmation was already obtained
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

            try {
                AirConsoleLogger.Log(() => $"Starting download from: {url}");

                using (var req = UnityWebRequest.Get(url)) {
                    // Enhanced headers for better compatibility
                    req.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater/1.0");
                    req.SetRequestHeader("Accept", "*/*");
                    req.timeout = 60; // Longer timeout for downloads

                    var op = req.SendWebRequest();

                    // Enhanced progress tracking with timeout detection
                    var startTime = DateTime.Now;
                    var lastProgressTime = startTime;
                    float lastProgress = 0f;

                    while (!op.isDone) {
                        var currentTime = DateTime.Now;
                        var currentProgress = req.downloadProgress;

                        // Update progress bar
                        EditorUtility.DisplayProgressBar("AirConsole",
                            $"{progressMessage} ({req.downloadedBytes} bytes)", currentProgress);

                        // Check for stalled downloads (no progress for 30 seconds)
                        if (currentProgress > lastProgress) {
                            lastProgressTime = currentTime;
                            lastProgress = currentProgress;
                        } else if ((currentTime - lastProgressTime).TotalSeconds > 30) {
                            AirConsoleLogger.LogWarning(() => "Download appears stalled, continuing to wait...");
                            lastProgressTime = currentTime; // Reset to avoid spam
                        }

                        // Overall timeout check (5 minutes)
                        if ((currentTime - startTime).TotalMinutes > 5) {
                            AirConsoleLogger.LogError(() => "Download timeout after 5 minutes");
                            req.Abort();
                            break;
                        }

                        System.Threading.Thread.Sleep(100); // Prevent tight loop
                    }

                    // Process the result
                    if (req.result != UnityWebRequest.Result.Success) {
                        string errorDetails = $"HTTP {req.responseCode}: {req.error}";

                        // Enhanced error reporting based on error type
                        switch (req.result) {
                            case UnityWebRequest.Result.ConnectionError:
                                errorDetails += " (Connection failed - check network connectivity)";
                                break;
                            case UnityWebRequest.Result.ProtocolError:
                                if (req.responseCode == 404) {
                                    errorDetails += " (File not found - the download URL may be invalid)";
                                } else if (req.responseCode == 403) {
                                    errorDetails += " (Access denied - authentication may be required)";
                                } else if (req.responseCode >= 500) {
                                    errorDetails += " (Server error - try again later)";
                                }
                                break;
                            case UnityWebRequest.Result.DataProcessingError:
                                errorDetails += " (Data processing failed - file may be corrupted)";
                                break;
                        }

                        AirConsoleLogger.LogError(() => $"Download failed: {errorDetails}");
                        return null;
                    }

                    // Extract content length
                    var headers = req.GetResponseHeaders();
                    if (headers != null) {
                        string contentLengthStr = GetHeaderValue(headers, "Content-Length", "content-length");
                        if (!string.IsNullOrEmpty(contentLengthStr) && long.TryParse(contentLengthStr, out contentLength)) {
                            long downloadedBytes = contentLength; // Store in local variable for lambda
                            AirConsoleLogger.Log(() => $"Download completed: {downloadedBytes} bytes");
                        } else {
                            contentLength = (long)req.downloadedBytes;
                            long downloadedBytes = contentLength; // Store in local variable for lambda
                            AirConsoleLogger.Log(() => $"Download completed: {downloadedBytes} bytes (from downloadedBytes)");
                        }
                    } else {
                        contentLength = (long)req.downloadedBytes;
                    }

                    var result = getResult(req.downloadHandler);

                    if (result == null) {
                        AirConsoleLogger.LogError(() => "Download completed but result processing failed");
                    }

                    return result;
                }
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Download exception: {ex.Message}");
                AirConsoleLogger.LogException(ex);
                return null;
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