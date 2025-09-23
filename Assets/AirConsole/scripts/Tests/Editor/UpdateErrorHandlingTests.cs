#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using NDream.AirConsole.Editor;
using System;
using UnityEngine;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Tests for error handling and logging in the update checker system
    /// </summary>
    public class UpdateErrorHandlingTests {
        private UpdateSettings _testSettings;

        [SetUp]
        public void SetUp() {
            // Create a test settings instance
            _testSettings = ScriptableObject.CreateInstance<UpdateSettings>();
        }

        [TearDown]
        public void TearDown() {
            if (_testSettings != null) {
                UnityEngine.Object.DestroyImmediate(_testSettings);
            }
        }

        [Test]
        public void IncrementFailedCheckCount_RecordsErrorInformation() {
            // Arrange
            string errorMessage = "Network timeout error";
            bool isNetworkError = true;

            // Act
            _testSettings.IncrementFailedCheckCount(errorMessage, isNetworkError);

            // Assert
            Assert.AreEqual(1, _testSettings.FailedCheckCount);
            Assert.AreEqual(errorMessage, _testSettings.LastErrorMessage);
            Assert.IsTrue(_testSettings.NetworkErrorDetected);
            Assert.IsTrue(_testSettings.LastErrorTime > DateTime.MinValue);
        }

        [Test]
        public void ResetFailedCheckCount_ClearsAllErrorState() {
            // Arrange
            _testSettings.IncrementFailedCheckCount("Test error", true);
            Assert.AreEqual(1, _testSettings.FailedCheckCount);

            // Act
            _testSettings.ResetFailedCheckCount();

            // Assert
            Assert.AreEqual(0, _testSettings.FailedCheckCount);
            Assert.AreEqual("", _testSettings.LastErrorMessage);
            Assert.IsFalse(_testSettings.NetworkErrorDetected);
            Assert.AreEqual(DateTime.MinValue, _testSettings.LastErrorTime);
        }

        [Test]
        public void GetDiagnosticInfo_ReturnsFormattedInformation() {
            // Arrange
            _testSettings.IncrementFailedCheckCount("Test network error", true);

            // Act
            string diagnosticInfo = _testSettings.GetDiagnosticInfo();

            // Assert
            Assert.IsNotNull(diagnosticInfo);
            Assert.IsTrue(diagnosticInfo.Contains("Failed checks: 1"));
            Assert.IsTrue(diagnosticInfo.Contains("Test network error"));
            Assert.IsTrue(diagnosticInfo.Contains("Network issues detected"));
        }

        [Test]
        public void ShouldAttemptRecovery_ReturnsTrueAfter24Hours() {
            // Arrange
            _testSettings.IncrementFailedCheckCount("Network error", true);

            // Simulate 24 hours passing by setting the error time to 25 hours ago
            var twentyFiveHoursAgo = DateTime.Now.AddHours(-25);
            _testSettings.LastErrorTime = twentyFiveHoursAgo;

            // Act
            bool shouldRecover = _testSettings.ShouldAttemptRecovery();

            // Assert
            Assert.IsTrue(shouldRecover);
        }

        [Test]
        public void ShouldAttemptRecovery_ReturnsFalseBeforeTimeout() {
            // Arrange
            _testSettings.IncrementFailedCheckCount("Network error", true);

            // Error time is recent (within 24 hours)
            _testSettings.LastErrorTime = DateTime.Now.AddHours(-1);

            // Act
            bool shouldRecover = _testSettings.ShouldAttemptRecovery();

            // Assert
            Assert.IsFalse(shouldRecover);
        }

        [Test]
        public void GetEffectiveCheckInterval_ImplementsExponentialBackoff() {
            // Test that failure count affects the time until next check
            // Set up conditions where we can measure the intervals properly

            _testSettings.AutomaticCheckEnabled = true;
            _testSettings.LastCheckTime = DateTime.Now; // Set to now so we get full intervals

            // Test the progression of intervals with increasing failures
            // We'll test by checking that each additional failure increases the required wait time

            // Start with 1 failure (multiplier 1 = 24h)
            _testSettings.FailedCheckCount = 1;
            var interval1 = _testSettings.TimeUntilNextCheck();

            // 2 failures (multiplier 2 = 48h)
            _testSettings.FailedCheckCount = 2;
            var interval2 = _testSettings.TimeUntilNextCheck();

            // 3 failures (multiplier 4 = 96h)
            _testSettings.FailedCheckCount = 3;
            var interval3 = _testSettings.TimeUntilNextCheck();

            // Verify exponential backoff is working - each should be roughly double the previous
            Assert.IsTrue(interval2 > interval1, $"2 failures should have longer interval than 1: {interval1.TotalHours}h -> {interval2.TotalHours}h");
            Assert.IsTrue(interval3 > interval2, $"3 failures should have longer interval than 2: {interval2.TotalHours}h -> {interval3.TotalHours}h");

            // Verify the approximate ratios (allowing for small timing differences)
            double ratio2to1 = interval2.TotalHours / interval1.TotalHours;
            double ratio3to2 = interval3.TotalHours / interval2.TotalHours;

            Assert.IsTrue(ratio2to1 >= 1.9 && ratio2to1 <= 2.1, $"Ratio 2:1 should be ~2.0, got {ratio2to1:F2}");
            Assert.IsTrue(ratio3to2 >= 1.9 && ratio3to2 <= 2.1, $"Ratio 3:2 should be ~2.0, got {ratio3to2:F2}");
        }
    }
}
#endif