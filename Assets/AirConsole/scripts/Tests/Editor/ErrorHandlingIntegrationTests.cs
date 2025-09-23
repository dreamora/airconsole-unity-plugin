#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using NDream.AirConsole.Editor;
using System;
using UnityEngine;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Integration tests for comprehensive error handling and logging
    /// </summary>
    public class ErrorHandlingIntegrationTests {
        [Test]
        public void UpdateChecker_GetDiagnosticInfo_ReturnsValidInformation() {
            // Act
            string diagnosticInfo = UpdateChecker.GetDiagnosticInfo();

            // Assert
            Assert.IsNotNull(diagnosticInfo);
            Assert.IsTrue(diagnosticInfo.Contains("Update Available:"));
            Assert.IsTrue(diagnosticInfo.Contains("Check In Progress:"));
            Assert.IsTrue(diagnosticInfo.Contains("Automatic Checking:"));
            Assert.IsTrue(diagnosticInfo.Contains("Current Version:"));
        }

        [Test]
        public void UpdateChecker_ResetErrorState_ClearsErrorsAndReenablesChecking() {
            // Arrange - simulate some failures
            UpdateSettings settings = UpdateSettings.Instance;
            settings.IncrementFailedCheckCount("Test error", true);
            settings.AutomaticCheckEnabled = false;

            // Act
            UpdateChecker.ResetErrorState();

            // Assert
            Assert.AreEqual(0, settings.FailedCheckCount);
            Assert.IsTrue(settings.AutomaticCheckEnabled);
            Assert.IsFalse(settings.NetworkErrorDetected);
        }

        [Test]
        public void UpdateSettings_ExponentialBackoff_IncreasesIntervalWithFailures() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.ResetFailedCheckCount();
            settings.LastCheckTime = DateTime.Now.AddHours(-13); // Ensure we can check initially

            // Act & Assert - Test exponential backoff
            TimeSpan initialInterval = settings.TimeUntilNextCheck();

            settings.IncrementFailedCheckCount("Error 1");
            TimeSpan interval1 = settings.TimeUntilNextCheck();

            settings.IncrementFailedCheckCount("Error 2");
            TimeSpan interval2 = settings.TimeUntilNextCheck();

            settings.IncrementFailedCheckCount("Error 3");
            TimeSpan interval3 = settings.TimeUntilNextCheck();

            // Verify exponential backoff is working
            Assert.IsTrue(interval1 >= initialInterval, "First failure should maintain or increase interval");
            Assert.IsTrue(interval2 > interval1, "Second failure should increase interval");
            Assert.IsTrue(interval3 > interval2, "Third failure should further increase interval");
        }

        [Test]
        public void UpdateSettings_NetworkErrorDetection_PersistsAcrossFailures() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.ResetFailedCheckCount();

            // Act - Simulate network errors
            settings.IncrementFailedCheckCount("Connection timeout", true);
            Assert.IsTrue(settings.NetworkErrorDetected, "Should detect network error on first failure");

            settings.IncrementFailedCheckCount("DNS resolution failed", true);
            Assert.IsTrue(settings.NetworkErrorDetected, "Should maintain network error flag");

            // Reset and verify it clears
            settings.ResetFailedCheckCount();
            Assert.IsFalse(settings.NetworkErrorDetected, "Should clear network error flag on reset");
        }

        [Test]
        public void UpdateSettings_RecoveryLogic_WorksAfterTimeDelay() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.ResetFailedCheckCount();

            // Simulate a network error
            settings.IncrementFailedCheckCount("Network error", true);
            Assert.IsFalse(settings.ShouldAttemptRecovery(), "Should not attempt recovery immediately");

            // Simulate 25 hours passing
            settings.LastErrorTime = DateTime.Now.AddHours(-25);

            // Act & Assert
            Assert.IsTrue(settings.ShouldAttemptRecovery(), "Should attempt recovery after 24+ hours");
        }
    }
}
#endif
