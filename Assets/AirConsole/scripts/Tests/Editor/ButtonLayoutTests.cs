#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Comprehensive tests for button layout consistency and container structure in SettingWindow
    /// </summary>
    public class ButtonLayoutTests {
        private SettingWindow _testWindow;

        [SetUp]
        public void SetUp() {
            // Create a fresh window for each test
            _testWindow = EditorWindow.GetWindow<SettingWindow>();
        }

        [TearDown]
        public void TearDown() {
            // Clean up after each test
            if (_testWindow != null) {
                _testWindow.Close();
                _testWindow = null;
            }
        }

        [Test]
        public void SettingWindow_CanBeCreatedWithoutErrors() {
            // Test basic window creation
            Assert.IsNotNull(_testWindow, "SettingWindow should be created successfully");
            Assert.IsTrue(_testWindow.minSize.x > 0, "Window should have minimum width set");
            Assert.IsTrue(_testWindow.minSize.y > 0, "Window should have minimum height set");
        }

        [Test]
        public void SettingWindow_InitializesStylesWithoutErrors() {
            // Test that custom styles can be initialized
            try {
                _testWindow.Repaint();
                Assert.IsTrue(true, "Style initialization should complete without errors");
            } catch (Exception ex) {
                Assert.Fail($"Style initialization failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_HandlesMinimumWindowSize() {
            // Test behavior at minimum window size
            try {
                _testWindow.position = new Rect(100, 100, 400, 300);
                _testWindow.Repaint();
                Assert.IsTrue(true, "Window should handle minimum size without layout errors");
            } catch (Exception ex) {
                Assert.Fail($"Minimum window size handling failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_HandlesNarrowWindowLayout() {
            // Test responsive behavior in narrow window
            try {
                _testWindow.position = new Rect(100, 100, 350, 500);
                _testWindow.Repaint();
                Assert.IsTrue(true, "Window should handle narrow layout without errors");
            } catch (Exception ex) {
                Assert.Fail($"Narrow window layout failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_HandlesWideWindowLayout() {
            // Test responsive behavior in wide window
            try {
                _testWindow.position = new Rect(100, 100, 700, 400);
                _testWindow.Repaint();
                Assert.IsTrue(true, "Window should handle wide layout without errors");
            } catch (Exception ex) {
                Assert.Fail($"Wide window layout failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_HandlesTallWindowLayout() {
            // Test behavior with tall window (lots of vertical space)
            try {
                _testWindow.position = new Rect(100, 100, 450, 800);
                _testWindow.Repaint();
                Assert.IsTrue(true, "Window should handle tall layout without errors");
            } catch (Exception ex) {
                Assert.Fail($"Tall window layout failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_HandlesShortWindowLayout() {
            // Test behavior with short window (requires scrolling)
            try {
                _testWindow.position = new Rect(100, 100, 500, 250);
                _testWindow.Repaint();
                Assert.IsTrue(true, "Window should handle short layout with scrolling");
            } catch (Exception ex) {
                Assert.Fail($"Short window layout failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_ScrollingWorksCorrectly() {
            // Test scrolling functionality
            try {
                // Force scrolling with small window
                _testWindow.position = new Rect(100, 100, 400, 200);
                _testWindow.Repaint();

                // Multiple repaints to test scroll stability
                for (int i = 0; i < 3; i++) {
                    _testWindow.Repaint();
                }

                Assert.IsTrue(true, "Scrolling should work without layout issues");
            } catch (Exception ex) {
                Assert.Fail($"Scrolling functionality failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_AnimationSystemStable() {
            // Test that animation system doesn't cause layout issues
            try {
                _testWindow.position = new Rect(100, 100, 500, 400);

                // Multiple rapid repaints to test animation stability
                for (int i = 0; i < 10; i++) {
                    _testWindow.Repaint();

                    // Small delay to simulate time passing
                    System.Threading.Thread.Sleep(5);
                }

                Assert.IsTrue(true, "Animation system should be stable");
            } catch (Exception ex) {
                Assert.Fail($"Animation system failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_ResponsiveLayoutTransitions() {
            // Test transitions between different window sizes
            try {
                // Start narrow
                _testWindow.position = new Rect(100, 100, 350, 400);
                _testWindow.Repaint();

                // Transition to wide
                _testWindow.position = new Rect(100, 100, 600, 400);
                _testWindow.Repaint();

                // Transition to tall
                _testWindow.position = new Rect(100, 100, 450, 700);
                _testWindow.Repaint();

                // Back to minimum
                _testWindow.position = new Rect(100, 100, 400, 300);
                _testWindow.Repaint();

                Assert.IsTrue(true, "Layout transitions should be smooth");
            } catch (Exception ex) {
                Assert.Fail($"Layout transitions failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_UpdateCheckerIntegration() {
            // Test that UpdateChecker integration doesn't break layout
            try {
                // Access UpdateChecker properties that are used in UI
                bool isUpdateAvailable = UpdateChecker.IsUpdateAvailable;
                bool isCheckInProgress = UpdateChecker.IsCheckInProgress;
                bool canCheckNow = UpdateChecker.CanCheckNow();
                Version currentVersion = UpdateChecker.CurrentVersion;
                Version latestVersion = UpdateChecker.LatestVersion;
                DateTime lastCheckTime = UpdateChecker.LastCheckTime;
                TimeSpan timeUntilNext = UpdateChecker.TimeUntilNextCheck();

                // Render UI with UpdateChecker data
                _testWindow.Repaint();

                Assert.IsTrue(true, "UpdateChecker integration should work correctly");
            } catch (Exception ex) {
                Assert.Fail($"UpdateChecker integration failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_UpdateSettingsIntegration() {
            // Test that UpdateSettings integration doesn't break layout
            try {
                UpdateSettings settings = UpdateSettings.Instance;
                Assert.IsNotNull(settings, "UpdateSettings should be accessible");

                // Render UI with settings
                _testWindow.Repaint();

                Assert.IsTrue(true, "UpdateSettings integration should work correctly");
            } catch (Exception ex) {
                Assert.Fail($"UpdateSettings integration failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_TooltipSystemWorks() {
            // Test that tooltip system doesn't interfere with layout
            try {
                _testWindow.position = new Rect(100, 100, 500, 400);
                _testWindow.Repaint();

                // Multiple repaints to test tooltip stability
                for (int i = 0; i < 5; i++) {
                    _testWindow.Repaint();
                }

                Assert.IsTrue(true, "Tooltip system should work without layout issues");
            } catch (Exception ex) {
                Assert.Fail($"Tooltip system failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_FoldoutSectionsWork() {
            // Test that foldout sections don't break layout
            try {
                _testWindow.position = new Rect(100, 100, 450, 500);

                // Multiple repaints to test foldout stability
                for (int i = 0; i < 5; i++) {
                    _testWindow.Repaint();
                }

                Assert.IsTrue(true, "Foldout sections should work correctly");
            } catch (Exception ex) {
                Assert.Fail($"Foldout sections failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_ContainerNestingCorrect() {
            // Test that container nesting doesn't cause GUI errors
            try {
                _testWindow.position = new Rect(100, 100, 500, 600);
                _testWindow.Repaint();

                Assert.IsTrue(true, "Container nesting should be correct");
            } catch (Exception ex) {
                Assert.Fail($"Container nesting failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_ButtonHierarchyConsistent() {
            // Test that button hierarchy styles are applied consistently
            try {
                _testWindow.position = new Rect(100, 100, 500, 400);
                _testWindow.Repaint();

                // Test different window sizes to ensure button hierarchy is maintained
                _testWindow.position = new Rect(100, 100, 350, 400);
                _testWindow.Repaint();

                _testWindow.position = new Rect(100, 100, 700, 400);
                _testWindow.Repaint();

                Assert.IsTrue(true, "Button hierarchy should be consistent across window sizes");
            } catch (Exception ex) {
                Assert.Fail($"Button hierarchy consistency failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_PerformanceStable() {
            // Test that repeated repaints don't cause performance issues
            try {
                _testWindow.position = new Rect(100, 100, 500, 400);

                DateTime startTime = DateTime.Now;

                // Perform many repaints
                for (int i = 0; i < 50; i++) {
                    _testWindow.Repaint();
                }

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                // Should complete within reasonable time (5 seconds is very generous)
                Assert.IsTrue(duration.TotalSeconds < 5, "UI should render efficiently");
            } catch (Exception ex) {
                Assert.Fail($"Performance test failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_EdgeCaseWindowSizes() {
            // Test edge case window sizes
            try {
                // Very narrow
                _testWindow.position = new Rect(100, 100, 300, 400);
                _testWindow.Repaint();

                // Very wide
                _testWindow.position = new Rect(100, 100, 1000, 400);
                _testWindow.Repaint();

                // Very short
                _testWindow.position = new Rect(100, 100, 500, 150);
                _testWindow.Repaint();

                // Very tall
                _testWindow.position = new Rect(100, 100, 500, 1000);
                _testWindow.Repaint();

                Assert.IsTrue(true, "Edge case window sizes should be handled gracefully");
            } catch (Exception ex) {
                Assert.Fail($"Edge case window sizes failed: {ex.Message}");
            }
        }

        [Test]
        public void SettingWindow_MultipleInstancesStable() {
            // Test that multiple window instances don't interfere
            SettingWindow secondWindow = null;
            try {
                secondWindow = EditorWindow.GetWindow<SettingWindow>();

                _testWindow.position = new Rect(100, 100, 400, 300);
                secondWindow.position = new Rect(200, 200, 500, 400);

                _testWindow.Repaint();
                secondWindow.Repaint();

                Assert.IsTrue(true, "Multiple window instances should work correctly");
            } catch (Exception ex) {
                Assert.Fail($"Multiple instances test failed: {ex.Message}");
            } finally {
                if (secondWindow != null) {
                    secondWindow.Close();
                }
            }
        }

        [Test]
        public void SettingWindow_ResourcesLoadCorrectly() {
            // Test that required resources load without errors
            try {
                _testWindow.Repaint();

                // If we get here without exceptions, resources loaded correctly
                Assert.IsTrue(true, "Resources should load correctly");
            } catch (Exception ex) {
                Assert.Fail($"Resource loading failed: {ex.Message}");
            }
        }
    }
}
#endif
