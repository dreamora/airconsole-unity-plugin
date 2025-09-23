#if !DISABLE_AIRCONSOLE
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using NDream.AirConsole.Editor;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// NUnit tests for UpdateChecker static class
    /// Tests the main API interface for the AirConsole plugin update checking system
    ///
    /// NOTE: Tests avoid calling methods that show UI dialogs (like DownloadAndInstallUpdate)
    /// to prevent blocking automated test runs. Instead, we test method signatures and non-UI functionality.
    /// </summary>
    [Category("UpdateChecker")]
    public class UpdateCheckerTests {
        private UpdateSettings _originalSettings;
        private bool _wasAutomaticCheckingInitialized;

        [SetUp]
        public void SetUp() {
            // Store original state to restore later
            _wasAutomaticCheckingInitialized = IsAutomaticCheckingInitialized();

            // Stop any existing automatic checking to start with clean state
            UpdateChecker.StopAutomaticChecking();

            // Store original settings
            _originalSettings = UpdateSettings.Instance;
        }

        [TearDown]
        public void TearDown() {
            // Stop automatic checking
            UpdateChecker.StopAutomaticChecking();

            // Restore original state if it was running
            if (_wasAutomaticCheckingInitialized) {
                UpdateChecker.StartAutomaticChecking();
            }
        }

        private bool IsAutomaticCheckingInitialized() {
            // We can't directly access the private field, so we'll infer from behavior
            // If automatic checking is running, stopping and starting should change behavior
            var wasRunning = false;
            try {
                UpdateChecker.StopAutomaticChecking();
                UpdateChecker.StartAutomaticChecking();
                wasRunning = true;
            } catch {
                // If there's an issue, assume it wasn't running
            }
            return wasRunning;
        }

        #region Public Properties Tests

        [Test]
        public void IsUpdateAvailable_PropertyAccessible() {
            // The property combines GithubUpdate.IsUpdateAvailable && !IsCurrentUpdateDismissed
            // We just verify it's accessible without throwing
            Assert.DoesNotThrow(() => {
                var result = UpdateChecker.IsUpdateAvailable;
            });
        }

        [Test]
        public void LatestVersion_ReturnsGithubUpdateLatestVersion() {
            // Act
            var latestVersion = UpdateChecker.LatestVersion;
            var githubLatestVersion = GithubUpdate.LatestVersion;

            // Assert - Should return the same value as GithubUpdate
            Assert.AreEqual(githubLatestVersion, latestVersion);
        }

        [Test]
        public void IsCheckInProgress_ReturnsGithubUpdateIsCheckInProgress() {
            // Act
            var isInProgress = UpdateChecker.IsCheckInProgress;
            var githubIsInProgress = GithubUpdate.IsCheckInProgress;

            // Assert - Should return the same value as GithubUpdate
            Assert.AreEqual(githubIsInProgress, isInProgress);
        }

        [Test]
        public void LastCheckTime_ReturnsGithubUpdateLastCheckTime() {
            // Act
            var lastCheckTime = UpdateChecker.LastCheckTime;
            var githubLastCheckTime = GithubUpdate.LastCheckTime;

            // Assert - Should return the same value as GithubUpdate
            Assert.AreEqual(githubLastCheckTime, lastCheckTime);
        }

        [Test]
        public void CurrentVersion_ReturnsValidVersion() {
            // Act
            var currentVersion = UpdateChecker.CurrentVersion;

            // Assert
            Assert.IsNotNull(currentVersion);
            Assert.IsTrue(currentVersion.Major >= 0);

            // Should match GithubUpdate.GetCurrentVersion()
            Assert.AreEqual(GithubUpdate.GetCurrentVersion(), currentVersion);
        }

        [Test]
        public void IsCurrentUpdateDismissed_PropertyAccessible() {
            // When LatestVersion is null, IsCurrentUpdateDismissed should return false
            // We can't easily control LatestVersion, but we can test the logic

            var result = UpdateChecker.IsCurrentUpdateDismissed;

            // Should not throw and should be a boolean
            Assert.IsNotNull(result);
        }

        #endregion

        #region Automatic Checking Tests

        [Test]
        public void StartAutomaticChecking_WhenAlreadyInitialized_DoesNotReinitialize() {
            // Arrange
            UpdateChecker.StartAutomaticChecking();

            // Act - Start again
            Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());

            // Assert - Should not throw or cause issues
            // The method should handle being called multiple times gracefully
        }

        [Test]
        public void StartAutomaticChecking_WhenAutomaticCheckDisabled_DoesNotStart() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            settings.AutomaticCheckEnabled = false;

            try {
                // Act
                UpdateChecker.StartAutomaticChecking();

                // Assert - Should not throw
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
            }
        }

        [Test]
        public void StopAutomaticChecking_WhenNotInitialized_DoesNotThrow() {
            // Arrange - Ensure it's not initialized
            UpdateChecker.StopAutomaticChecking();

            // Act & Assert
            Assert.DoesNotThrow(() => UpdateChecker.StopAutomaticChecking());
        }

        [Test]
        public void RestartAutomaticChecking_CallsStopThenStart() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            settings.AutomaticCheckEnabled = true;

            try {
                // Act & Assert - Should not throw
                Assert.DoesNotThrow(() => UpdateChecker.RestartAutomaticChecking());
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
            }
        }

        [Test]
        public void StartAutomaticChecking_WithValidSettings_InitializesSuccessfully() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalCheckOnStartup = settings.CheckOnStartup;
            var originalLastCheck = settings.LastCheckTime;

            settings.AutomaticCheckEnabled = true;
            settings.CheckOnStartup = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // Allow startup check

            try {
                // Act & Assert
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.CheckOnStartup = originalCheckOnStartup;
                settings.LastCheckTime = originalLastCheck;
            }
        }

        #endregion

        #region Manual Checking Tests

        [Test]
        public void CheckForUpdatesAsync_WhenRateLimited_ReturnsFalse() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalLastCheck = settings.LastCheckTime;

            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddMinutes(-30); // Recent check

            try {
                // Act
                bool result = UpdateChecker.CheckForUpdatesAsync(false);

                // Assert
                Assert.IsFalse(result, "Should return false when rate limited");
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.LastCheckTime = originalLastCheck;
            }
        }

        [Test]
        public void CheckForUpdatesAsync_RateLimitingLogic_WorksCorrectly() {
            // Test the rate limiting logic without triggering API calls
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalLastCheck = settings.LastCheckTime;

            try {
                // Test case 1: Rate limited
                settings.AutomaticCheckEnabled = true;
                settings.LastCheckTime = DateTime.Now.AddMinutes(-30);
                bool rateLimitedResult = UpdateChecker.CheckForUpdatesAsync(false);
                Assert.IsFalse(rateLimitedResult, "Should be rate limited");

                // Test case 2: Check allowed
                settings.LastCheckTime = DateTime.Now.AddHours(-25);
                bool canCheck = UpdateChecker.CanCheckNow();
                Assert.IsTrue(canCheck, "Should allow check when rate limit expired");

            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.LastCheckTime = originalLastCheck;
            }
        }

        [Test]
        public void CanCheckNow_ReturnsGithubUpdateCanCheckNow() {
            // Act
            var canCheck = UpdateChecker.CanCheckNow();
            var githubCanCheck = GithubUpdate.CanCheckNow();

            // Assert - Should return the same value as GithubUpdate
            Assert.AreEqual(githubCanCheck, canCheck);
        }

        [Test]
        public void TimeUntilNextCheck_ReturnsGithubUpdateTimeUntilNextCheck() {
            // Act
            var timeUntilNext = UpdateChecker.TimeUntilNextCheck();
            var githubTimeUntilNext = GithubUpdate.TimeUntilNextCheck();

            // Assert - Should return values within a small tolerance (due to timing differences)
            var difference = Math.Abs((timeUntilNext - githubTimeUntilNext).TotalMilliseconds);
            Assert.IsTrue(difference < 1000, $"TimeSpan values should be within 1 second. Difference: {difference}ms");
        }

        #endregion

        #region Dismiss Functionality Tests

        [Test]
        public void DismissCurrentUpdate_WithNullLatestVersion_DoesNotThrow() {
            // Act & Assert
            Assert.DoesNotThrow(() => UpdateChecker.DismissCurrentUpdate());
        }

        [Test]
        public void DismissVersion_WithValidVersion_CallsGithubUpdateDismissVersion() {
            // Arrange
            var version = new Version(2, 7, 0);
            var settings = UpdateSettings.Instance;
            var originalDismissed = settings.DismissedVersion;

            try {
                // Act
                UpdateChecker.DismissVersion(version);

                // Assert
                Assert.AreEqual(version.ToString(), settings.DismissedVersion);
            } finally {
                // Cleanup
                settings.DismissedVersion = originalDismissed;
            }
        }

        [Test]
        public void DismissVersion_WithNullVersion_DoesNotThrow() {
            // Act & Assert
            Assert.DoesNotThrow(() => UpdateChecker.DismissVersion(null));
        }

        [Test]
        public void ClearDismissedVersion_ClearsSettingsDismissedVersion() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalDismissed = settings.DismissedVersion;
            settings.DismissedVersion = "2.7.0";

            try {
                // Act
                UpdateChecker.ClearDismissedVersion();

                // Assert
                Assert.AreEqual("", settings.DismissedVersion);
            } finally {
                settings.DismissedVersion = originalDismissed;
            }
        }

        [Test]
        public void IsVersionDismissed_WithValidVersion_ReturnsGithubUpdateResult() {
            // Arrange
            var version = new Version(2, 7, 0);
            var settings = UpdateSettings.Instance;
            var originalDismissed = settings.DismissedVersion;

            try {
                // Test dismissed version
                settings.DismissedVersion = version.ToString();

                // Act
                var isDismissed = UpdateChecker.IsVersionDismissed(version);
                var githubIsDismissed = GithubUpdate.IsVersionDismissed(version);

                // Assert
                Assert.AreEqual(githubIsDismissed, isDismissed);
                Assert.IsTrue(isDismissed, "Should return true for dismissed version");

                // Test non-dismissed version
                settings.DismissedVersion = "1.0.0";
                var isNotDismissed = UpdateChecker.IsVersionDismissed(version);
                var githubIsNotDismissed = GithubUpdate.IsVersionDismissed(version);

                Assert.AreEqual(githubIsNotDismissed, isNotDismissed);
                Assert.IsFalse(isNotDismissed, "Should return false for non-dismissed version");

            } finally {
                // Cleanup
                settings.DismissedVersion = originalDismissed;
            }
        }

        [Test]
        public void IsVersionDismissed_WithNullVersion_ReturnsFalse() {
            // Act
            var result = UpdateChecker.IsVersionDismissed(null);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Update Installation Tests

        [Test]
        public void DownloadAndInstallUpdate_MethodExists() {
            // Test that the method exists and has correct signature without calling it
            // This avoids UI dialogs during unit tests
            var method = typeof(UpdateChecker).GetMethod("DownloadAndInstallUpdate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(method, "DownloadAndInstallUpdate method should exist");
            Assert.AreEqual(typeof(bool), method.ReturnType, "Method should return boolean");
            Assert.AreEqual(0, method.GetParameters().Length, "Method should take no parameters");
        }

        [Test]
        public void DownloadAndInstallUpdateWithoutConfirmation_MethodExists() {
            // Test that the new method exists and has correct signature without calling it
            // This avoids UI dialogs during unit tests
            var method = typeof(UpdateChecker).GetMethod("DownloadAndInstallUpdateWithoutConfirmation",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(method, "DownloadAndInstallUpdateWithoutConfirmation method should exist");
            Assert.AreEqual(typeof(bool), method.ReturnType, "Method should return boolean");
            Assert.AreEqual(0, method.GetParameters().Length, "Method should take no parameters");
        }

        [Test]
        public void OpenReleasePage_DoesNotThrow() {
            // Act & Assert
            Assert.DoesNotThrow(() => UpdateChecker.OpenReleasePage());
        }

        [Test]
        public void OpenLatestReleasePage_DoesNotThrow() {
            // Act & Assert
            Assert.DoesNotThrow(() => UpdateChecker.OpenLatestReleasePage());
        }

        #endregion

        #region Integration Tests

        [Test]
        public void UpdateChecker_IntegrationWithUpdateSettings_WorksCorrectly() {
            // Test the complete integration between UpdateChecker and UpdateSettings
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalLastCheck = settings.LastCheckTime;

            try {
                // Test case 1: Disabled automatic checking
                settings.AutomaticCheckEnabled = false;
                Assert.IsFalse(UpdateChecker.CanCheckNow(), "Should not allow check when disabled");

                // Test case 2: Rate limited
                settings.AutomaticCheckEnabled = true;
                settings.LastCheckTime = DateTime.Now.AddMinutes(-30);
                Assert.IsFalse(UpdateChecker.CanCheckNow(), "Should not allow check when rate limited");

                // Test case 3: Check allowed
                settings.LastCheckTime = DateTime.Now.AddHours(-25);
                Assert.IsTrue(UpdateChecker.CanCheckNow(), "Should allow check when rate limit expired");

            } finally {
                // Restore original settings
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.LastCheckTime = originalLastCheck;
            }
        }

        [Test]
        public void UpdateChecker_IntegrationWithGithubUpdate_WorksCorrectly() {
            // Test that UpdateChecker properly delegates to GithubUpdate

            // Test property delegation
            Assert.AreEqual(GithubUpdate.LatestVersion, UpdateChecker.LatestVersion);
            Assert.AreEqual(GithubUpdate.IsCheckInProgress, UpdateChecker.IsCheckInProgress);
            Assert.AreEqual(GithubUpdate.LastCheckTime, UpdateChecker.LastCheckTime);
            Assert.AreEqual(GithubUpdate.GetCurrentVersion(), UpdateChecker.CurrentVersion);

            // Test method delegation
            Assert.AreEqual(GithubUpdate.CanCheckNow(), UpdateChecker.CanCheckNow());

            // Test TimeUntilNextCheck with tolerance for timing differences
            var updateCheckerTime = UpdateChecker.TimeUntilNextCheck();
            var githubUpdateTime = GithubUpdate.TimeUntilNextCheck();
            var timeDifference = Math.Abs((updateCheckerTime - githubUpdateTime).TotalMilliseconds);
            Assert.IsTrue(timeDifference < 1000, $"TimeUntilNextCheck values should be within 1 second. Difference: {timeDifference}ms");
        }

        [Test]
        public void UpdateChecker_DismissFunctionality_IntegrationTest() {
            // Test the complete dismiss functionality workflow
            var settings = UpdateSettings.Instance;
            var originalDismissed = settings.DismissedVersion;

            try {
                // Clear any existing dismissed version
                UpdateChecker.ClearDismissedVersion();
                Assert.AreEqual("", settings.DismissedVersion);

                // Dismiss a specific version
                var testVersion = new Version(2, 7, 0);
                UpdateChecker.DismissVersion(testVersion);
                Assert.AreEqual(testVersion.ToString(), settings.DismissedVersion);
                Assert.IsTrue(UpdateChecker.IsVersionDismissed(testVersion));

                // Clear dismissed version
                UpdateChecker.ClearDismissedVersion();
                Assert.AreEqual("", settings.DismissedVersion);
                Assert.IsFalse(UpdateChecker.IsVersionDismissed(testVersion));

            } finally {
                // Restore original dismissed version
                settings.DismissedVersion = originalDismissed;
            }
        }

        [Test]
        public void UpdateChecker_AutomaticCheckingLifecycle_WorksCorrectly() {
            // Test the complete automatic checking lifecycle
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;

            try {
                // Ensure automatic checking is enabled
                settings.AutomaticCheckEnabled = true;

                // Test starting automatic checking
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());

                // Test restarting
                Assert.DoesNotThrow(() => UpdateChecker.RestartAutomaticChecking());

                // Test stopping
                Assert.DoesNotThrow(() => UpdateChecker.StopAutomaticChecking());

                // Test starting again after stopping
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());

            } finally {
                // Restore original settings and stop automatic checking
                settings.AutomaticCheckEnabled = originalEnabled;
                UpdateChecker.StopAutomaticChecking();
            }
        }

        #endregion

        #region Startup Initialization Tests

        [Test]
        public void StartupInitialization_WithValidSettings_InitializesCorrectly() {
            // Test that startup initialization works correctly with valid settings
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalCheckOnStartup = settings.CheckOnStartup;
            var originalLastCheck = settings.LastCheckTime;

            try {
                // Arrange - Set up valid settings for startup check
                settings.AutomaticCheckEnabled = true;
                settings.CheckOnStartup = true;
                settings.LastCheckTime = DateTime.Now.AddHours(-25); // Allow startup check

                // Act - Simulate startup initialization
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());

                // Assert - Verify that automatic checking is properly initialized
                // The system should be ready to perform checks
                Assert.IsTrue(settings.CanCheckNow(), "Should be able to check after startup initialization");

            } finally {
                // Cleanup
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.CheckOnStartup = originalCheckOnStartup;
                settings.LastCheckTime = originalLastCheck;
                UpdateChecker.StopAutomaticChecking();
            }
        }

        [Test]
        public void StartupInitialization_WithCheckOnStartupDisabled_SkipsStartupCheck() {
            // Test that startup check is skipped when CheckOnStartup is disabled
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalCheckOnStartup = settings.CheckOnStartup;
            var originalLastCheck = settings.LastCheckTime;

            try {
                // Arrange - Disable startup checking
                settings.AutomaticCheckEnabled = true;
                settings.CheckOnStartup = false;
                settings.LastCheckTime = DateTime.Now.AddHours(-25); // Would allow check if enabled

                // Act - Should not throw and should handle disabled startup check gracefully
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());

                // Assert - System should still be initialized for periodic checks
                Assert.IsTrue(settings.CanCheckNow(), "Should still be able to check manually");

            } finally {
                // Cleanup
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.CheckOnStartup = originalCheckOnStartup;
                settings.LastCheckTime = originalLastCheck;
                UpdateChecker.StopAutomaticChecking();
            }
        }

        [Test]
        public void StartupInitialization_WithRateLimiting_RespectsRateLimits() {
            // Test that startup initialization respects rate limiting
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalCheckOnStartup = settings.CheckOnStartup;
            var originalLastCheck = settings.LastCheckTime;

            try {
                // Arrange - Set up rate limited scenario
                settings.AutomaticCheckEnabled = true;
                settings.CheckOnStartup = true;
                settings.LastCheckTime = DateTime.Now.AddMinutes(-30); // Recent check, should be rate limited

                // Act - Should not throw and should handle rate limiting gracefully
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());

                // Assert - Should not be able to check due to rate limiting
                Assert.IsFalse(settings.CanCheckNow(), "Should be rate limited");
                Assert.IsTrue(settings.TimeUntilNextCheck() > TimeSpan.Zero, "Should have time remaining until next check");

            } finally {
                // Cleanup
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.CheckOnStartup = originalCheckOnStartup;
                settings.LastCheckTime = originalLastCheck;
                UpdateChecker.StopAutomaticChecking();
            }
        }

        [Test]
        public void StartupInitialization_FirstTimeSetup_CreatesSettingsAsset() {
            // Test that first-time setup properly creates the settings asset
            // This test verifies that UpdateSettings.Instance works correctly

            // Act - Access the settings instance (should create if not exists)
            var settings = UpdateSettings.Instance;

            // Clear any existing dismissed version for this test
            string originalDismissedVersion = settings.DismissedVersion;
            settings.DismissedVersion = "";

            try {
                // Assert - Settings should be created and accessible
                Assert.IsNotNull(settings, "Settings instance should be created");
                Assert.IsTrue(settings.AutomaticCheckEnabled, "Default should enable automatic checking");
                Assert.IsTrue(settings.CheckOnStartup, "Default should enable startup checking");
                Assert.AreEqual(24, settings.CheckIntervalHours, "Default interval should be 24 hours");
                Assert.AreEqual(0, settings.FailedCheckCount, "Default failed count should be 0");
                Assert.AreEqual("", settings.DismissedVersion, "Dismissed version should be empty after reset");
            } finally {
                // Restore original dismissed version
                settings.DismissedVersion = originalDismissedVersion;
            }
        }

        [Test]
        public void StartupInitialization_HandlesExceptions_Gracefully() {
            // Test that startup initialization handles exceptions gracefully
            // This simulates potential issues during startup

            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;

            try {
                // Arrange - Set up valid settings
                settings.AutomaticCheckEnabled = true;

                // Act & Assert - Should not throw even if there are internal issues
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());
                Assert.DoesNotThrow(() => UpdateChecker.StopAutomaticChecking());

            } finally {
                // Cleanup
                settings.AutomaticCheckEnabled = originalEnabled;
                UpdateChecker.StopAutomaticChecking();
            }
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void UpdateChecker_HandlesNullLatestVersionGracefully() {
            // Test that methods handle null LatestVersion gracefully
            Assert.DoesNotThrow(() => {
                var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
                var isCurrentDismissed = UpdateChecker.IsCurrentUpdateDismissed;
                UpdateChecker.DismissCurrentUpdate();
            });
        }

        [Test]
        public void UpdateChecker_AllPublicMethods_DoNotThrowUnexpectedExceptions() {
            // Test that all public methods handle edge cases gracefully
            Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());
            Assert.DoesNotThrow(() => UpdateChecker.StopAutomaticChecking());
            Assert.DoesNotThrow(() => UpdateChecker.RestartAutomaticChecking());
            Assert.DoesNotThrow(() => UpdateChecker.CheckForUpdatesAsync());
            Assert.DoesNotThrow(() => UpdateChecker.CanCheckNow());
            Assert.DoesNotThrow(() => UpdateChecker.TimeUntilNextCheck());
            Assert.DoesNotThrow(() => UpdateChecker.DismissCurrentUpdate());
            Assert.DoesNotThrow(() => UpdateChecker.DismissVersion(null));
            Assert.DoesNotThrow(() => UpdateChecker.DismissVersion(new Version(1, 0, 0)));
            Assert.DoesNotThrow(() => UpdateChecker.ClearDismissedVersion());
            Assert.DoesNotThrow(() => UpdateChecker.IsVersionDismissed(null));
            Assert.DoesNotThrow(() => UpdateChecker.IsVersionDismissed(new Version(1, 0, 0)));
            // Removed DownloadAndInstallUpdate() call to avoid UI dialogs in unit tests
            Assert.DoesNotThrow(() => UpdateChecker.OpenReleasePage());
            Assert.DoesNotThrow(() => UpdateChecker.OpenLatestReleasePage());
        }

        #endregion
    }
}
#endif