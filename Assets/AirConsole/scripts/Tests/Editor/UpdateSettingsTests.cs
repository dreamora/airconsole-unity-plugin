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
            Assert.AreEqual(12, settings.CheckIntervalHours, "Default check interval should be 12 hours");
            Assert.IsTrue(settings.CheckOnStartup, "Check on startup should be enabled by default");
            Assert.AreEqual("", settings.DismissedVersion, "Dismissed version should be empty by default");
            Assert.AreEqual(0, settings.FailedCheckCount, "Failed check count should be 0 by default");
            Assert.AreEqual(DateTime.MinValue, settings.LastCheckTime, "Last check time should be MinValue by default");
        }

        [Test]
        public void CanCheckNow_ReturnsFalse_WhenAutomaticCheckDisabled() {
            // Test requirement 4.4: Disabled automatic checking
            settings.AutomaticCheckEnabled = false;
            settings.LastCheckTime = DateTime.Now.AddHours(-13); // More than 12 hours ago

            Assert.IsFalse(settings.CanCheckNow(), "Should not be able to check when automatic checking is disabled");
        }

        [Test]
        public void CanCheckNow_ReturnsTrue_WhenEnoughTimeHasPassed() {
            // Test requirement 4.2: Time-based rate limiting
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-13); // 13 hours ago (> 12 hour minimum)

            Assert.IsTrue(settings.CanCheckNow(), "Should be able to check when more than 12 hours have passed");
        }

        [Test]
        public void CanCheckNow_ReturnsFalse_WhenNotEnoughTimeHasPassed() {
            // Test requirement 4.2: Time-based rate limiting
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-6); // 6 hours ago (< 12 hour minimum)

            Assert.IsFalse(settings.CanCheckNow(), "Should not be able to check when less than 12 hours have passed");
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
            settings.LastCheckTime = DateTime.Now.AddHours(-13); // 13 hours ago

            Assert.AreEqual(TimeSpan.Zero, settings.TimeUntilNextCheck(), "Should return zero when check is allowed");
        }

        [Test]
        public void TimeUntilNextCheck_ReturnsCorrectTime_WhenCheckNotAllowed() {
            // Test requirement 4.3: Time constraint helpers
            settings.AutomaticCheckEnabled = true;
            settings.LastCheckTime = DateTime.Now.AddHours(-1); // 1 hour ago (need to wait 11 more hours)

            TimeSpan timeUntilNext = settings.TimeUntilNextCheck();
            Assert.IsTrue(timeUntilNext.TotalHours > 10 && timeUntilNext.TotalHours < 12,
                $"Should return approximately 11 hours, got {timeUntilNext.TotalHours}");
        }

        [Test]
        public void TimeUntilNextCheck_ReturnsMaxValue_WhenAutomaticCheckDisabled() {
            // Test requirement 4.4: Disabled automatic checking
            settings.AutomaticCheckEnabled = false;

            Assert.AreEqual(TimeSpan.MaxValue, settings.TimeUntilNextCheck(),
                "Should return MaxValue when automatic checking is disabled");
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
            string originalDismissedVersion = settings.DismissedVersion;

            try {
                settings.DismissedVersion = "2.7.0";
                Assert.AreEqual("2.7.0", settings.DismissedVersion, "Should store dismissed version");

                settings.DismissedVersion = null;
                Assert.AreEqual("", settings.DismissedVersion, "Should handle null as empty string");
            } finally {
                // Restore original state (though this is a fresh instance, it's good practice)
                settings.DismissedVersion = originalDismissedVersion;
            }
        }

        [Test]
        public void LastCheckTime_CanBeSetAndRetrieved() {
            // Test requirement 4.5: Settings persistence
            DateTime testTime = new(2023, 12, 25, 10, 30, 0);
            settings.LastCheckTime = testTime;

            Assert.AreEqual(testTime, settings.LastCheckTime, "Should store and retrieve last check time accurately");
        }

        [Test]
        public void Properties_TriggerDirtyMarking() {
            // Test that property setters mark the object as dirty (can't directly test EditorUtility.SetDirty in tests)
            // This is more of a behavioral test to ensure the setters are called

            bool originalEnabled = settings.AutomaticCheckEnabled;
            settings.AutomaticCheckEnabled = !originalEnabled;
            Assert.AreNotEqual(originalEnabled, settings.AutomaticCheckEnabled, "AutomaticCheckEnabled should change");
        }
    }
}
#endif
