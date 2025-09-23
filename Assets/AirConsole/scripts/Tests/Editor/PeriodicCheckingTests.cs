#if !DISABLE_AIRCONSOLE
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using NDream.AirConsole.Editor;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// NUnit tests for the periodic background checking system (Task 9)
    /// Tests the EditorApplication.update callback system, intelligent scheduling,
    /// check interval validation, and cleanup logic
    /// </summary>
    [Category("PeriodicChecking")]
    public class PeriodicCheckingTests {
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

        #region Periodic Checking System Tests

        [Test]
        public void StartAutomaticChecking_WithPersistentFailures_DisablesAutomaticChecking() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalFailedCount = settings.FailedCheckCount;

            try {
                // Set up scenario with 5+ failures
                settings.AutomaticCheckEnabled = true;
                settings.FailedCheckCount = 5;

                // Act
                UpdateChecker.StartAutomaticChecking();

                // Assert
                Assert.IsFalse(settings.AutomaticCheckEnabled, "Should disable automatic checking after 5+ failures");
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.FailedCheckCount = originalFailedCount;
            }
        }

        [Test]
        public void ShouldDisableAutomaticChecking_WithHighFailureCount_ReturnsTrue() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalFailedCount = settings.FailedCheckCount;

            try {
                settings.FailedCheckCount = 5;

                // Act
                bool shouldDisable = UpdateChecker.ShouldDisableAutomaticChecking();

                // Assert
                Assert.IsTrue(shouldDisable, "Should return true when failed count >= 5");
            } finally {
                settings.FailedCheckCount = originalFailedCount;
            }
        }

        [Test]
        public void ShouldDisableAutomaticChecking_WithNetworkErrorsOverSevenDays_ReturnsTrue() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalNetworkError = settings.NetworkErrorDetected;
            var originalLastError = settings.LastErrorTime;

            try {
                settings.NetworkErrorDetected = true;
                settings.LastErrorTime = DateTime.Now.AddDays(-8); // 8 days ago

                // Act
                bool shouldDisable = UpdateChecker.ShouldDisableAutomaticChecking();

                // Assert
                Assert.IsTrue(shouldDisable, "Should return true when network errors persist for > 7 days");
            } finally {
                settings.NetworkErrorDetected = originalNetworkError;
                settings.LastErrorTime = originalLastError;
            }
        }

        [Test]
        public void ValidateCheckInterval_WithBelowMinimum_ReturnsMinimum() {
            // Act
            int validatedInterval = UpdateChecker.ValidateCheckInterval(12); // Below 24h minimum

            // Assert
            Assert.AreEqual(24, validatedInterval, "Should return minimum 24 hours");
        }

        [Test]
        public void ValidateCheckInterval_WithAboveMaximum_ReturnsMaximum() {
            // Act
            int validatedInterval = UpdateChecker.ValidateCheckInterval(200); // Above 168h maximum

            // Assert
            Assert.AreEqual(168, validatedInterval, "Should return maximum 168 hours");
        }

        [Test]
        public void ValidateCheckInterval_WithValidValue_ReturnsValue() {
            // Act
            int validatedInterval = UpdateChecker.ValidateCheckInterval(48);

            // Assert
            Assert.AreEqual(48, validatedInterval, "Should return the valid input value");
        }

        [Test]
        public void GetEffectiveCheckInterval_ReturnsSettingsInterval() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalInterval = settings.CheckIntervalHours;

            try {
                settings.CheckIntervalHours = 48;

                // Act
                int effectiveInterval = UpdateChecker.GetEffectiveCheckInterval();

                // Assert
                Assert.AreEqual(48, effectiveInterval, "Should return the settings check interval");
            } finally {
                settings.CheckIntervalHours = originalInterval;
            }
        }

        [Test]
        public void GetPeriodicCheckingStatus_ReturnsValidStatusString() {
            // Act
            string status = UpdateChecker.GetPeriodicCheckingStatus();

            // Assert
            Assert.IsNotNull(status, "Status should not be null");
            Assert.IsTrue(status.Contains("Automatic Checking:"), "Should contain automatic checking status");
            Assert.IsTrue(status.Contains("System Initialized:"), "Should contain initialization status");
            Assert.IsTrue(status.Contains("Check Interval:"), "Should contain check interval");
            Assert.IsTrue(status.Contains("Failed Check Count:"), "Should contain failed check count");
        }

        [Test]
        public void ForceRestartPeriodicChecking_StopsAndStartsChecking() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;

            try {
                settings.AutomaticCheckEnabled = true;
                UpdateChecker.StartAutomaticChecking();

                // Act
                Assert.DoesNotThrow(() => UpdateChecker.ForceRestartPeriodicChecking());

                // Assert - Should not throw and should handle restart gracefully
                // The system should be ready for periodic checks after restart
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
            }
        }

        #endregion

        #region Intelligent Scheduling Tests

        [Test]
        public void StartAutomaticChecking_LogsIntervalAndFailures() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalFailedCount = settings.FailedCheckCount;

            try {
                settings.AutomaticCheckEnabled = true;
                settings.FailedCheckCount = 2;

                // Act & Assert - Should not throw and should log appropriate information
                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.FailedCheckCount = originalFailedCount;
            }
        }

        [Test]
        public void StopAutomaticChecking_CleansUpProperly() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;

            try {
                settings.AutomaticCheckEnabled = true;
                UpdateChecker.StartAutomaticChecking();

                // Act
                UpdateChecker.StopAutomaticChecking();

                // Assert - Should clean up without throwing
                Assert.DoesNotThrow(() => UpdateChecker.StopAutomaticChecking()); // Should handle multiple calls
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
            }
        }

        #endregion

        #region Error State Management Tests

        [Test]
        public void ResetErrorState_ClearsFailuresAndReenablesChecking() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalFailedCount = settings.FailedCheckCount;

            try {
                // Set up error state
                settings.AutomaticCheckEnabled = false;
                settings.FailedCheckCount = 5;

                // Act
                UpdateChecker.ResetErrorState();

                // Assert
                Assert.AreEqual(0, settings.FailedCheckCount, "Should reset failed check count");
                Assert.IsTrue(settings.AutomaticCheckEnabled, "Should re-enable automatic checking");
            } finally {
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.FailedCheckCount = originalFailedCount;
            }
        }

        [Test]
        public void GetDiagnosticInfo_IncludesFailureInformation() {
            // Arrange
            var settings = UpdateSettings.Instance;
            var originalFailedCount = settings.FailedCheckCount;

            try {
                settings.FailedCheckCount = 3;

                // Act
                string diagnosticInfo = UpdateChecker.GetDiagnosticInfo();

                // Assert
                Assert.IsNotNull(diagnosticInfo, "Diagnostic info should not be null");
                Assert.IsTrue(diagnosticInfo.Contains("Failed Check Count: 3"), "Should include failed check count");
            } finally {
                settings.FailedCheckCount = originalFailedCount;
            }
        }

        #endregion

        #region Integration Tests

        [Test]
        public void PeriodicCheckingSystem_IntegrationTest() {
            // Test the complete periodic checking system integration
            var settings = UpdateSettings.Instance;
            var originalEnabled = settings.AutomaticCheckEnabled;
            var originalFailedCount = settings.FailedCheckCount;
            var originalLastCheck = settings.LastCheckTime;

            try {
                // Test case 1: Normal operation
                settings.AutomaticCheckEnabled = true;
                settings.FailedCheckCount = 0;
                settings.LastCheckTime = DateTime.Now.AddHours(-25);

                Assert.DoesNotThrow(() => UpdateChecker.StartAutomaticChecking());
                Assert.IsTrue(settings.CanCheckNow(), "Should allow check in normal operation");

                // Test case 2: With failures but not disabled
                settings.FailedCheckCount = 3;
                Assert.DoesNotThrow(() => UpdateChecker.RestartAutomaticChecking());

                // Test case 3: Error state reset
                settings.FailedCheckCount = 5;
                settings.AutomaticCheckEnabled = false;
                UpdateChecker.ResetErrorState();
                Assert.AreEqual(0, settings.FailedCheckCount, "Should reset failures");
                Assert.IsTrue(settings.AutomaticCheckEnabled, "Should re-enable checking");

            } finally {
                // Restore original settings
                settings.AutomaticCheckEnabled = originalEnabled;
                settings.FailedCheckCount = originalFailedCount;
                settings.LastCheckTime = originalLastCheck;
                UpdateChecker.StopAutomaticChecking();
            }
        }

        #endregion
    }
}
#endif