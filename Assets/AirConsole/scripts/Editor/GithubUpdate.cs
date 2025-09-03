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

        internal static bool UpdateCheckInProgress => _updateCheckInProgress;
        internal static bool UpdateAvailable => _updateAvailable;
        internal static Version LatestVersion => _latestVersion;

        internal static void BeginBackgroundUpdateCheck() {
            if (_updateCheckStarted || _updateCheckInProgress) {
                return;
            }

            _updateCheckStarted = true;
            _updateCheckInProgress = true;
            try {
                _checkRequest = UnityWebRequest.Get(GithubLatestAPI);
                _checkRequest.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                _checkOp = _checkRequest.SendWebRequest();
                EditorApplication.update += PollUpdateCheck;
            } catch (Exception e) {
                _updateCheckInProgress = false;
                AirConsoleLogger.LogError(() => $"Failed to start update check: {e.Message}");
            }
        }

        private static void PollUpdateCheck() {
            if (_checkOp == null) {
                return;
            }

            bool done = _checkOp.isDone && _checkRequest.result != UnityWebRequest.Result.InProgress;
            if (!done) {
                return;
            }

            try {
                if (_checkRequest.result == UnityWebRequest.Result.Success) {
                    string json = _checkRequest.downloadHandler.text;
                    GithubRelease release = JsonUtility.FromJson<GithubRelease>(json);
                    _cachedLatestRelease = release;

                    if (TryParseVersionFromTag(release?.tag_name ?? release?.name, out var latest) &&
                        Version.TryParse(Settings.VERSION, out var current)) {
                        _latestVersion = latest;
                        _updateAvailable = latest > current;
                    } else {
                        _updateAvailable = false;
                    }
                } else {
                    AirConsoleLogger.LogWarning(() => $"Update check failed: {_checkRequest.responseCode} {_checkRequest.error}");
                    _updateAvailable = false;
                }
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Update check error: {ex.Message}");
                _updateAvailable = false;
            } finally {
                _updateCheckInProgress = false;
                _checkRequest.Dispose();
                _checkRequest = null;
                _checkOp = null;
                EditorApplication.update -= PollUpdateCheck;
                InternalEditorUtility.RepaintAllViews();
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
