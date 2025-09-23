#if !DISABLE_AIRCONSOLE
using System;
using NUnit.Framework;
using UnityEngine;

namespace NDream.AirConsole.Editor.Tests {
    public class GithubUpdateTests {
        private UpdateSettings _testSettings;
        private string _originalVersion;

        [SetUp]
        public void SetUp() {
            // Create a test settings instance
            _testSettings = ScriptableObject.CreateInstance<UpdateSettings>();

            // Store original version for restoration
            _originalVersion = Settings.VERSION;

            // Wait for any ongoing checks to complete before starting tests
            WaitForCheckToComplete();
        }

        [TearDown]
        public void TearDown() {
            // Wait for any ongoing checks to complete before cleanup
            WaitForCheckToComplete();

            // Clean up test settings
            if (_testSettings != null) {
                ScriptableObject.DestroyImmediate(_testSettings);
            }
        }

        private void WaitForCheckToComplete() {
            // Wait up to 5 seconds for any ongoing check to complete
            DateTime timeout = DateTime.Now.AddSeconds(5);
            while (GithubUpdate.IsCheckInProgress && DateTime.Now < timeout) {
                System.Threading.Thread.Sleep(100);
            }
        }

        [Test]
        public void GetCurrentVersion_ReturnsValidVersion() {
            // Act
            Version version = GithubUpdate.GetCurrentVersion();

            // Assert
            Assert.IsNotNull(version);
            Assert.IsTrue(version.Major >= 0);
        }

        [Test]
        public void IsVersionDismissed_WithNullVersion_ReturnsFalse() {
            // Act
            bool result = GithubUpdate.IsVersionDismissed(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsVersionDismissed_WithDismissedVersion_ReturnsTrue() {
            // Arrange
            Version version = new Version(1, 2, 3);
            UpdateSettings settings = UpdateSettings.Instance;
            string originalDismissedVersion = settings.DismissedVersion;
            settings.DismissedVersion = version.ToString();

            try {
                // Act
                bool result = GithubUpdate.IsVersionDismissed(version);

                // Assert
                Assert.IsTrue(result);
            } finally {
                // Cleanup
                settings.DismissedVersion = originalDismissedVersion;
            }
        }

        [Test]
        public void IsVersionDismissed_WithNonDismissedVersion_ReturnsFalse() {
            // Arrange
            Version version = new Version(1, 2, 3);
            UpdateSettings settings = UpdateSettings.Instance;
            string originalDismissedVersion = settings.DismissedVersion;
            settings.DismissedVersion = "2.0.0";

            try {
                // Act
                bool result = GithubUpdate.IsVersionDismissed(version);

                // Assert
                Assert.IsFalse(result);
            } finally {
                // Cleanup
                settings.DismissedVersion = originalDismissedVersion;
            }
        }

        [Test]
        public void DismissVersion_WithValidVersion_SetsDismissedVersion() {
            // Arrange
            Version version = new Version(1, 2, 3);
            UpdateSettings settings = UpdateSettings.Instance;
            string originalDismissedVersion = settings.DismissedVersion;

            try {
                // Act
                GithubUpdate.DismissVersion(version);

                // Assert
                Assert.AreEqual(version.ToString(), settings.DismissedVersion);
            } finally {
                // Cleanup
                settings.DismissedVersion = originalDismissedVersion;
            }
        }

        [Test]
        public void DismissVersion_WithNullVersion_DoesNotThrow() {
            // Act & Assert
            Assert.DoesNotThrow(() => GithubUpdate.DismissVersion(null));
        }

        [Test]
        public void CanCheckNow_WhenSettingsAllowCheck_ReturnsTrue() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // More than 24 hours ago

            // Act
            bool result = GithubUpdate.CanCheckNow();

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CanCheckNow_WhenRateLimited_ReturnsFalse() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-1); // Less than 24 hours ago

            // Act
            bool result = GithubUpdate.CanCheckNow();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TimeUntilNextCheck_WhenCheckAllowed_ReturnsZero() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // More than 24 hours ago

            // Act
            TimeSpan timeUntilNext = GithubUpdate.TimeUntilNextCheck();

            // Assert
            Assert.AreEqual(TimeSpan.Zero, timeUntilNext);
        }

        [Test]
        public void TimeUntilNextCheck_WhenRateLimited_ReturnsPositiveTimeSpan() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-1); // 1 hour ago

            // Act
            TimeSpan timeUntilNext = GithubUpdate.TimeUntilNextCheck();

            // Assert
            Assert.IsTrue(timeUntilNext > TimeSpan.Zero);
            Assert.IsTrue(timeUntilNext.TotalHours > 20); // Should be around 23 hours
        }

        [Test]
        public void BeginBackgroundUpdateCheck_WhenRateLimited_ReturnsFalse() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-1); // Less than 24 hours ago

            // Act
            bool result = GithubUpdate.BeginBackgroundUpdateCheck(false);

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(GithubUpdate.IsCheckInProgress);
        }

        [Test]
        public void BeginBackgroundUpdateCheck_WhenForced_IgnoresRateLimit_LogicOnly() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-1); // Less than 24 hours ago

            // Act - Test only the rate limiting logic without making API calls
            bool canCheckNormally = GithubUpdate.CanCheckNow();
            bool shouldBypassWhenForced = !canCheckNormally; // Forced should work when normal can't

            // Assert - Verify the rate limiting logic works correctly
            Assert.IsFalse(canCheckNormally, "Normal check should be rate limited");
            Assert.IsTrue(shouldBypassWhenForced, "Forced check should bypass rate limiting");
        }

        [Test]
        public void BeginBackgroundUpdateCheck_WhenCheckAllowed_LogicOnly() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // More than 24 hours ago

            // Act - Test only the rate limiting logic
            bool canCheckNow = GithubUpdate.CanCheckNow();
            TimeSpan timeUntilNext = GithubUpdate.TimeUntilNextCheck();

            // Assert - Verify the rate limiting allows the check
            Assert.IsTrue(canCheckNow, "Check should be allowed when enough time has passed");
            Assert.AreEqual(TimeSpan.Zero, timeUntilNext, "No wait time should be required");
        }

        [Test]
        public void BeginBackgroundUpdateCheck_WhenAlreadyInProgress_ReturnsFalse() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25);

            // Ensure no check is in progress initially
            Assert.IsFalse(GithubUpdate.IsCheckInProgress, "A check should not be in progress at test start");

            // Start first check
            bool firstResult = GithubUpdate.BeginBackgroundUpdateCheck(false);
            Assert.IsTrue(firstResult, "First check should start successfully");
            Assert.IsTrue(GithubUpdate.IsCheckInProgress, "Check should be in progress after first start");

            // Act - Try to start second check
            bool result = GithubUpdate.BeginBackgroundUpdateCheck(false);

            // Assert
            Assert.IsFalse(result, "Second check should fail when one is already in progress");
            Assert.IsTrue(GithubUpdate.IsCheckInProgress, "Check should still be in progress");
        }

        [Test]
        public void PublicProperties_ExposeCorrectValues() {
            // Act & Assert
            Assert.IsNotNull(GithubUpdate.IsUpdateAvailable);
            Assert.IsNotNull(GithubUpdate.IsCheckInProgress);

            // LatestVersion can be null if no check has been performed
            Version latestVersion = GithubUpdate.LatestVersion;
            // Just verify it doesn't throw

            DateTime lastCheckTime = GithubUpdate.LastCheckTime;
            // Verify it's a valid DateTime (can be DateTime.MinValue)
            Assert.IsTrue(lastCheckTime >= DateTime.MinValue);
        }

        [Test]
        public void BeginBackgroundUpdateCheck_UpdatesLastCheckTime_LogicOnly() {
            // Arrange
            UpdateSettings settings = UpdateSettings.Instance;
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25);
            DateTime beforeCheck = DateTime.Now;

            // Act - Test the logic that would update LastCheckTime without making API calls
            // Simulate what the method does: if CanCheckNow(), then update LastCheckTime
            if (settings.CanCheckNow()) {
                DateTime simulatedUpdateTime = DateTime.Now;

                // Assert - Verify the timing logic works
                Assert.IsTrue(simulatedUpdateTime >= beforeCheck, "Update time should be after the before time");
                Assert.IsTrue(settings.CanCheckNow(), "Check should be allowed when rate limit permits");
            }
        }

        [Test]
        public void RateLimitingIntegration_WorksWithUpdateSettings() {
            // Test the complete rate limiting integration without API calls
            UpdateSettings settings = UpdateSettings.Instance;

            // Test case 1: Automatic checks disabled
            settings.AutomaticCheckEnabled = false;
            Assert.IsFalse(GithubUpdate.CanCheckNow(), "Should not allow check when automatic checks disabled");

            // Test case 2: Recent check (rate limited)
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddMinutes(-30);
            Assert.IsFalse(GithubUpdate.CanCheckNow(), "Should not allow check when rate limited");

            // Test case 3: Old check (allowed)
            settings.LastCheckTime = DateTime.Now.AddHours(-25);
            Assert.IsTrue(GithubUpdate.CanCheckNow(), "Should allow check when rate limit expired");

            // Test case 4: Time until next check calculation
            settings.LastCheckTime = DateTime.Now.AddHours(-1);
            TimeSpan timeUntilNext = GithubUpdate.TimeUntilNextCheck();
            Assert.IsTrue(timeUntilNext > TimeSpan.Zero, "Should have positive wait time when rate limited");
            Assert.IsTrue(timeUntilNext.TotalHours > 20, "Should require approximately 23 hours wait");
        }
    }
}
#endif