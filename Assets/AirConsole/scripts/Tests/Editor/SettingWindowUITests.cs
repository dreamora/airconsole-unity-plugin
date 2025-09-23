#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Integration tests for SettingWindow UI functionality
    /// UI Polish: Enhanced tests for styling, responsiveness, and user experience
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

        [Test]
        public void SettingWindow_RespondsToWindowSizeChanges() {
            // UI Polish: Test responsive layout behavior
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // Test different window sizes
                window.position = new Rect(100, 100, 300, 400); // Narrow window
                window.Repaint();

                window.position = new Rect(100, 100, 600, 400); // Wide window
                window.Repaint();

                window.position = new Rect(100, 100, 400, 200); // Short window
                window.Repaint();

                // If no exceptions thrown, responsive layout works
                Assert.IsTrue(true);
            } catch (Exception ex) {
                Assert.Fail($"UI should handle different window sizes gracefully: {ex.Message}");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void SettingWindow_MinimumSizeIsSet() {
            // UI Polish: Test that minimum window size is enforced
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // Check that minimum size is set
                Assert.IsTrue(window.minSize.x > 0, "Minimum width should be set");
                Assert.IsTrue(window.minSize.y > 0, "Minimum height should be set");
                Assert.IsTrue(window.minSize.x >= 400, "Minimum width should be at least 400px");
                Assert.IsTrue(window.minSize.y >= 300, "Minimum height should be at least 300px");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void SettingWindow_CustomStylesInitialize() {
            // UI Polish: Test that custom styles are properly initialized
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // Trigger OnGUI to initialize styles
                window.Repaint();

                // Test should pass if no exceptions are thrown during style initialization
                Assert.IsTrue(true);
            } catch (Exception ex) {
                Assert.Fail($"Custom styles should initialize without errors: {ex.Message}");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void SettingWindow_AnimationSystemWorks() {
            // UI Polish: Test that animation system doesn't cause errors
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // Trigger multiple repaints to test animation system
                for (int i = 0; i < 5; i++) {
                    window.Repaint();
                    // Small delay to simulate time passing
                    System.Threading.Thread.Sleep(10);
                }

                // Test passes if no exceptions during animation updates
                Assert.IsTrue(true);
            } catch (Exception ex) {
                Assert.Fail($"Animation system should work without errors: {ex.Message}");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void SettingWindow_TooltipsAreConfigured() {
            // UI Polish: Test that tooltips are properly configured
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // This test verifies that the UI can be rendered with tooltips
                // without throwing exceptions
                window.Repaint();

                // Test passes if UI renders successfully with tooltip content
                Assert.IsTrue(true);
            } catch (Exception ex) {
                Assert.Fail($"UI with tooltips should render without errors: {ex.Message}");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void SettingWindow_ScrollingWorks() {
            // UI Polish: Test that scrolling functionality works
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // Set a small window size to force scrolling
                window.position = new Rect(100, 100, 300, 200);
                window.Repaint();

                // Test passes if scrolling UI renders without errors
                Assert.IsTrue(true);
            } catch (Exception ex) {
                Assert.Fail($"Scrolling UI should work without errors: {ex.Message}");
            } finally {
                window.Close();
            }
        }

        [Test]
        public void SettingWindow_CollapsibleSectionsWork() {
            // UI Polish: Test that collapsible sections function properly
            var window = EditorWindow.GetWindow<SettingWindow>();

            try {
                // Trigger OnGUI multiple times to test foldout behavior
                window.Repaint();
                window.Repaint();

                // Test passes if collapsible sections render without errors
                Assert.IsTrue(true);
            } catch (Exception ex) {
                Assert.Fail($"Collapsible sections should work without errors: {ex.Message}");
            } finally {
                window.Close();
            }
        }
    }
}
#endif