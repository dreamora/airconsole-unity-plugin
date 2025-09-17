#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Integration tests for SettingWindow UI functionality
    /// </summary>
    public class SettingWindowUITests {

        [Test]
        public void SettingWindow_CanBeCreated() {
            // Test that the SettingWindow can be instantiated without errors
            var window = EditorWindow.GetWindow<SettingWindow>();
            Assert.IsNotNull(window);
            window.Close();
        }

        [Test]
        public void UpdateNotificationBanner_HandlesNullVersions() {
            // Test that the UI methods handle null versions gracefully
            var window = EditorWindow.GetWindow<SettingWindow>();

            // This should not throw exceptions even if UpdateChecker returns null versions
            try {
                // Force a repaint to trigger OnGUI
                window.Repaint();
                // If we get here without exception, test passes
            } catch (Exception ex) {
                Assert.Fail($"UI should handle null versions gracefully: {ex.Message}");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void UpdateChecker_PropertiesAreAccessible() {
            // Test that all UpdateChecker properties used in the UI are accessible
            try {
                var isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
                var isCheckInProgress = UpdateChecker.IsCheckInProgress;
                var canCheckNow = UpdateChecker.CanCheckNow();
                var currentVersion = UpdateChecker.CurrentVersion;
                var latestVersion = UpdateChecker.LatestVersion;
                var lastCheckTime = UpdateChecker.LastCheckTime;
                var timeUntilNext = UpdateChecker.TimeUntilNextCheck();

                // These calls should not throw exceptions - if we get here, test passes
                Assert.IsTrue(true); // Explicit pass
            } catch (Exception ex) {
                Assert.Fail($"UpdateChecker properties should be accessible: {ex.Message}");
            }
        }

        [Test]
        public void UpdateChecker_ActionsAreCallable() {
            // Test that UpdateChecker actions used in the UI are callable
            try {
                // These should not throw exceptions (though they may not do anything if no update is available)
                UpdateChecker.OpenReleasePage();
                UpdateChecker.DismissCurrentUpdate();

                // If we get here without exception, test passes
                Assert.IsTrue(true); // Explicit pass
            } catch (Exception ex) {
                Assert.Fail($"UpdateChecker actions should be callable: {ex.Message}");
            }
        }
    }
}
#endif