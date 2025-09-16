#if !DISABLE_AIRCONSOLE
using System;
using NUnit.Framework;
using UnityEngine;
using NDream.AirConsole.Editor;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// NUnit tests for UpdateSettings ScriptableObject
    /// </summary>
    public class UpdateSettingsTests {
        private UpdateSettings settings;

        [SetUp]
        public void SetUp() {
            // Create a fresh instance for each test to avoid state pollution
            settings = ScriptableObject.CreateInstance<UpdateSettings>();
        }

        [TearDown]
        public void TearDown() {
            if (settings != null) {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void DefaultValues_AreSetCorrectly() {
            // Test requirement 4.1: Default settings should be reasonable
            Assert.IsTrue(settings.AutomaticCheckEnabled, "Automatic checking should be enabled by default");
            Assert.AreEqual(24, settings.CheckIntervalHours, "Default check interval should be 24 hours");
            Assert.IsTrue(settings.CheckOnStartup, "Check on startup should be enabled by default");
            Assert.AreEqual("", settings.DismissedVersion, "Dismissed version should be empty by default");
            Assert.AreEqual(0, settings.FailedCheckCount, "Failed check count should be 0 by default");
            Assert.AreEqual(DateTime.MinValue, settings.LastCheckTime, "Last check time should be MinValue by default");
        }

        [Test]
        public void CheckIntervalHours_EnforcesMinimum24Hours() {
            // Test requirement 4.2: Minimum 24-hour interval validation
            settings.CheckIntervalHours = 12;
            Assert.AreEqual(24, settings.CheckIntervalHours, "Check interval should be clamped to minimum 24 hours");

            settings.CheckIntervalHours = 1;
            Assert.AreEqual(24, settings.CheckIntervalHours, "Check interval should be clamped to minimum 24 hours");

            settings.CheckIntervalHours = 48;
            Assert.AreEqual(48, settings.CheckIntervalHours, "Check interval should accept values >= 24 hours");
        }

        [Test]
        public void CanCheckNow_ReturnsFalse_WhenAutomaticCheckDisabled() {
            // Test requirement 4.4: Disabled automatic checking
            settings.AutomaticCheckEnabled = false;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // More than 24 hours ago

            Assert.IsFalse(settings.CanCheckNow(), "Should not be able to check when automatic checking is disabled");
        }

        [Test]
        public void CanCheckNow_ReturnsTrue_WhenEnoughTimeHasPassed() {
            // Test requirement 4.2: Time-based rate limiting
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // 25 hours ago (> 24 hour minimum)

            Assert.IsTrue(settings.CanCheckNow(), "Should be able to check when more than 24 hours have passed");
        }

        [Test]
        public void CanCheckNow_ReturnsFalse_WhenNotEnoughTimeHasPassed() {
            // Test requirement 4.2: Time-based rate limiting
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-12); // 12 hours ago (< 24 hour minimum)

            Assert.IsFalse(settings.CanCheckNow(), "Should not be able to check when less than 24 hours have passed");
        }

        [Test]
        public void CanCheckNow_ReturnsTrue_WhenNeverCheckedBefore() {
            // Test initial state behavior
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.MinValue; // Never checked

            Assert.IsTrue(settings.CanCheckNow(), "Should be able to check when never checked before");
        }

        [Test]
        public void TimeUntilNextCheck_ReturnsZero_WhenCheckAllowed() {
            // Test requirement 4.3: Time constraint helpers
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // 25 hours ago

            Assert.AreEqual(TimeSpan.Zero, settings.TimeUntilNextCheck(), "Should return zero when check is allowed");
        }

        [Test]
        public void TimeUntilNextCheck_ReturnsCorrectTime_WhenCheckNotAllowed() {
            // Test requirement 4.3: Time constraint helpers
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-12); // 12 hours ago

            var timeUntilNext = settings.TimeUntilNextCheck();
            Assert.IsTrue(timeUntilNext.TotalHours > 11 && timeUntilNext.TotalHours < 13,
                $"Should return approximately 12 hours, got {timeUntilNext.TotalHours}");
        }

        [Test]
        public void TimeUntilNextCheck_ReturnsMaxValue_WhenAutomaticCheckDisabled() {
            // Test requirement 4.4: Disabled automatic checking
            settings.AutomaticCheckEnabled = false;

            Assert.AreEqual(TimeSpan.MaxValue, settings.TimeUntilNextCheck(),
                "Should return MaxValue when automatic checking is disabled");
        }

        [Test]
        public void ExponentialBackoff_IncreasesInterval_OnFailures() {
            // Test requirement 4.2: Rate limiting with exponential backoff
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-25); // 25 hours ago

            // No failures - should be able to check
            settings.ResetFailedCheckCount();
            Assert.IsTrue(settings.CanCheckNow(), "Should be able to check with no failures");

            // 1 failure - still 24 hour interval, should be able to check
            settings.IncrementFailedCheckCount();
            Assert.IsTrue(settings.CanCheckNow(), "Should be able to check with 1 failure after 25 hours");

            // 2 failures - 48 hour interval, should not be able to check after only 25 hours
            settings.IncrementFailedCheckCount();
            Assert.IsFalse(settings.CanCheckNow(), "Should not be able to check with 2 failures after only 25 hours");

            // But should be able to check after 49 hours
            settings.LastCheckTime = DateTime.Now.AddHours(-49);
            Assert.IsTrue(settings.CanCheckNow(), "Should be able to check with 2 failures after 49 hours");
        }

        [Test]
        public void FailedCheckCount_CanBeResetAndIncremented() {
            // Test failure count management
            Assert.AreEqual(0, settings.FailedCheckCount, "Should start with 0 failures");

            settings.IncrementFailedCheckCount();
            Assert.AreEqual(1, settings.FailedCheckCount, "Should increment to 1");

            settings.IncrementFailedCheckCount();
            Assert.AreEqual(2, settings.FailedCheckCount, "Should increment to 2");

            settings.ResetFailedCheckCount();
            Assert.AreEqual(0, settings.FailedCheckCount, "Should reset to 0");
        }

        [Test]
        public void FailedCheckCount_CannotBeNegative() {
            // Test validation
            settings.FailedCheckCount = -5;
            Assert.AreEqual(0, settings.FailedCheckCount, "Failed check count should not be negative");
        }

        [Test]
        public void DismissedVersion_CanBeSetAndRetrieved() {
            // Test requirement 4.5: Settings persistence
            settings.DismissedVersion = "2.7.0";
            Assert.AreEqual("2.7.0", settings.DismissedVersion, "Should store dismissed version");

            settings.DismissedVersion = null;
            Assert.AreEqual("", settings.DismissedVersion, "Should handle null as empty string");
        }

        [Test]
        public void LastCheckTime_CanBeSetAndRetrieved() {
            // Test requirement 4.5: Settings persistence
            var testTime = new DateTime(2023, 12, 25, 10, 30, 0);
            settings.LastCheckTime = testTime;

            Assert.AreEqual(testTime, settings.LastCheckTime, "Should store and retrieve last check time accurately");
        }

        [Test]
        public void Properties_TriggerDirtyMarking() {
            // Test that property setters mark the object as dirty (can't directly test EditorUtility.SetDirty in tests)
            // This is more of a behavioral test to ensure the setters are called

            var originalEnabled = settings.AutomaticCheckEnabled;
            settings.AutomaticCheckEnabled = !originalEnabled;
            Assert.AreNotEqual(originalEnabled, settings.AutomaticCheckEnabled, "AutomaticCheckEnabled should change");

            var originalInterval = settings.CheckIntervalHours;
            settings.CheckIntervalHours = originalInterval + 24;
            Assert.AreNotEqual(originalInterval, settings.CheckIntervalHours, "CheckIntervalHours should change");
        }
    }
}
#endif