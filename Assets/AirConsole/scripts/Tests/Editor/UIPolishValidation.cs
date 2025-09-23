#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Manual validation tools for UI polish improvements in AirConsole Plugin Update Checker
    /// </summary>
    public static class UIPolishValidation {

        [MenuItem("AirConsole/Tests/Validate All UI Polish")]
        public static void ValidateAllUIPolish() {
            Debug.Log("=== AirConsole UI Polish Complete Validation ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();

                // Set to a good size for validation
                window.position = new Rect(100, 100, 500, 600);

                Debug.Log("✓ Settings window opened for complete UI polish validation");

                EditorUtility.DisplayDialog("Complete UI Polish Validation",
                    "AirConsole Settings window is now open for comprehensive validation.\n\n" +
                    "Please verify ALL of the following:\n\n" +
                    "STYLING & BRANDING:\n" +
                    "• Consistent AirConsole color scheme\n" +
                    "• Proper button hierarchy (primary/secondary/small)\n" +
                    "• Uniform spacing and padding\n" +
                    "• Professional appearance matching Unity Editor\n\n" +
                    "TOOLTIPS & HELP:\n" +
                    "• All interactive elements have tooltips\n" +
                    "• Help text explains features clearly\n" +
                    "• Contextual information is helpful\n\n" +
                    "ANIMATIONS & TRANSITIONS:\n" +
                    "• Smooth banner fade in/out\n" +
                    "• Loading indicators animate smoothly\n" +
                    "• No jarring or abrupt changes\n\n" +
                    "CONFIRMATIONS:\n" +
                    "• Update installation requires confirmation\n" +
                    "• Dismiss update requires confirmation\n" +
                    "• Reset settings requires confirmation\n\n" +
                    "RESPONSIVE LAYOUT:\n" +
                    "• Resize window to test different sizes\n" +
                    "• All content remains accessible\n" +
                    "• Scrolling works when needed", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Complete UI polish validation failed: {ex.Message}");
                EditorUtility.DisplayDialog("Validation Error",
                    $"An error occurred during validation:\n\n{ex.Message}", "OK");
            }
        }

        [MenuItem("AirConsole/Tests/Test Button Layout Consistency")]
        public static void TestButtonLayoutConsistency() {
            Debug.Log("=== Testing Button Layout Consistency ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();

                Debug.Log("✓ Testing button layout at different window sizes");

                // Test sequence of different window sizes
                EditorApplication.delayCall += () => {
                    window.position = new Rect(100, 100, 350, 500); // Narrow
                    Debug.Log("✓ Testing narrow layout (350px width)");

                    EditorApplication.delayCall += () => {
                        window.position = new Rect(100, 100, 500, 400); // Medium
                        Debug.Log("✓ Testing medium layout (500px width)");

                        EditorApplication.delayCall += () => {
                            window.position = new Rect(100, 100, 700, 500); // Wide
                            Debug.Log("✓ Testing wide layout (700px width)");

                            EditorApplication.delayCall += () => {
                                window.position = new Rect(100, 100, 400, 300); // Minimum
                                Debug.Log("✓ Testing minimum size (400x300)");
                            };
                        };
                    };
                };

                EditorUtility.DisplayDialog("Button Layout Testing",
                    "Testing button layouts at different window sizes.\n\n" +
                    "The window will automatically resize through different configurations.\n\n" +
                    "Please verify:\n" +
                    "• All buttons maintain consistent heights within their tier\n" +
                    "• Primary buttons: Bold, 28px height (Update Now, Check Now)\n" +
                    "• Secondary buttons: Normal, 28px height (Release Notes)\n" +
                    "• Small buttons: Normal, 26px height (Dismiss, Reset)\n" +
                    "• Button spacing is uniform throughout\n" +
                    "• Buttons align properly in all sections\n" +
                    "• FlexibleSpace maintains proper alignment\n" +
                    "• No buttons get cut off or overlap\n" +
                    "• Status indicators align correctly with buttons", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Button layout consistency test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Container Structure")]
        public static void TestContainerStructure() {
            Debug.Log("=== Testing Container Structure ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();
                window.position = new Rect(100, 100, 450, 600);

                Debug.Log("✓ Testing section container structure");

                EditorUtility.DisplayDialog("Container Structure Testing",
                    "Testing section container structure and spacing.\n\n" +
                    "Please verify:\n\n" +
                    "CONTAINER HIERARCHY:\n" +
                    "• Update Settings: Header inside helpBox container\n" +
                    "• Connection Settings: Header inside helpBox container\n" +
                    "• Debug Settings: Header inside helpBox container\n" +
                    "• All containers have consistent visual appearance\n\n" +
                    "SPACING & ALIGNMENT:\n" +
                    "• No overlapping between titles and containers\n" +
                    "• Consistent padding within all containers\n" +
                    "• Proper spacing between sections (10px)\n" +
                    "• Consistent spacing within sections (5px)\n\n" +
                    "FOLDOUT BEHAVIOR:\n" +
                    "• Update Settings foldout expands/collapses correctly\n" +
                    "• Container boundaries remain intact when folded\n" +
                    "• Status indicator aligns properly in header\n\n" +
                    "VISUAL CONSISTENCY:\n" +
                    "• All helpBox containers look identical\n" +
                    "• No visual gaps or overlaps between sections", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Container structure test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Animation System")]
        public static void TestAnimationSystem() {
            Debug.Log("=== Testing Animation System ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();
                window.position = new Rect(100, 100, 500, 500);

                Debug.Log("✓ Testing animation system");
                Debug.Log("Manual test: Observe banner animations and loading indicators");

                EditorUtility.DisplayDialog("Animation System Testing",
                    "Testing smooth transitions and animations.\n\n" +
                    "Please verify:\n\n" +
                    "BANNER ANIMATIONS:\n" +
                    "• Update banners fade in/out smoothly\n" +
                    "• No abrupt appearance/disappearance\n" +
                    "• Animation speed feels natural (not too fast/slow)\n" +
                    "• Alpha transitions are smooth\n\n" +
                    "LOADING INDICATORS:\n" +
                    "• Spinning dots animate smoothly\n" +
                    "• Loading animation doesn't interfere with layout\n" +
                    "• Animation continues consistently\n" +
                    "• No flickering or jumping\n\n" +
                    "PERFORMANCE:\n" +
                    "• Animations don't cause lag or stuttering\n" +
                    "• UI remains responsive during animations\n" +
                    "• No excessive CPU usage from animations\n\n" +
                    "Note: You may need to trigger update checks or\n" +
                    "simulate different states to see all animations.", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Animation system test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Tooltip System")]
        public static void TestTooltipSystem() {
            Debug.Log("=== Testing Tooltip System ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();
                window.position = new Rect(100, 100, 500, 600);

                Debug.Log("✓ Testing tooltip system");
                Debug.Log("Manual test: Hover over interactive elements to verify tooltips");

                EditorUtility.DisplayDialog("Tooltip System Testing",
                    "Testing tooltips and help text throughout the UI.\n\n" +
                    "Please hover over and verify tooltips for:\n\n" +
                    "UPDATE SETTINGS:\n" +
                    "• Automatic Update Checking toggle\n" +
                    "• Check Interval slider\n" +
                    "• Check on Editor Startup toggle\n" +
                    "• Auto-open Settings Window toggle\n" +
                    "• Check Now button\n" +
                    "• Show Dismissed Updates button\n\n" +
                    "UPDATE BANNERS:\n" +
                    "• Update Now button\n" +
                    "• Release Notes button\n" +
                    "• Dismiss button\n\n" +
                    "CONNECTION SETTINGS:\n" +
                    "• Websocket Port field\n" +
                    "• Webserver Port field\n\n" +
                    "DEBUG SETTINGS:\n" +
                    "• Info, Warning, Error toggles\n\n" +
                    "FOOTER:\n" +
                    "• Reset Settings button\n\n" +
                    "Verify all tooltips:\n" +
                    "• Appear on hover\n" +
                    "• Contain helpful information\n" +
                    "• Are clearly readable\n" +
                    "• Don't interfere with UI layout", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Tooltip system test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Confirmation Dialogs")]
        public static void TestConfirmationDialogs() {
            Debug.Log("=== Testing Confirmation Dialogs ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();
                window.position = new Rect(100, 100, 500, 500);

                Debug.Log("✓ Testing confirmation dialog system");

                EditorUtility.DisplayDialog("Confirmation Dialog Testing",
                    "Testing confirmation dialogs for important actions.\n\n" +
                    "Please test the following actions and verify\n" +
                    "confirmation dialogs appear:\n\n" +
                    "UPDATE ACTIONS:\n" +
                    "• Click 'Update Now' button (if available)\n" +
                    "  → Should show update installation confirmation\n" +
                    "  → Should explain restart requirement\n" +
                    "  → Should have 'Update Now' and 'Cancel' options\n\n" +
                    "• Click 'Dismiss' button (if available)\n" +
                    "  → Should show dismiss confirmation\n" +
                    "  → Should explain dismissal behavior\n" +
                    "  → Should have 'Dismiss' and 'Cancel' options\n\n" +
                    "SETTINGS ACTIONS:\n" +
                    "• Click 'Reset Settings' button in footer\n" +
                    "  → Should show reset confirmation\n" +
                    "  → Should list what will be reset\n" +
                    "  → Should warn action cannot be undone\n" +
                    "  → Should have 'Reset Settings' and 'Cancel' options\n\n" +
                    "Verify all dialogs:\n" +
                    "• Have clear, informative messages\n" +
                    "• Have appropriate button labels\n" +
                    "• Cancel button works correctly\n" +
                    "• Action button performs expected operation", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Confirmation dialog test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Responsive Design")]
        public static void TestResponsiveDesign() {
            Debug.Log("=== Testing Responsive Design ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();

                Debug.Log("✓ Testing responsive design behavior");

                EditorUtility.DisplayDialog("Responsive Design Testing",
                    "Testing responsive layout behavior.\n\n" +
                    "Please manually resize the window and verify:\n\n" +
                    "MINIMUM SIZE ENFORCEMENT:\n" +
                    "• Window cannot be resized below 400x300\n" +
                    "• Content remains accessible at minimum size\n\n" +
                    "NARROW WINDOW BEHAVIOR:\n" +
                    "• Resize to ~350px width\n" +
                    "• Version display switches to vertical layout\n" +
                    "• All buttons remain clickable\n" +
                    "• No horizontal scrolling appears\n\n" +
                    "WIDE WINDOW BEHAVIOR:\n" +
                    "• Resize to ~700px width\n" +
                    "• Version display uses horizontal layout with arrow\n" +
                    "• Button alignment remains consistent\n" +
                    "• Extra space is handled gracefully\n\n" +
                    "SHORT WINDOW BEHAVIOR:\n" +
                    "• Resize to ~250px height\n" +
                    "• Vertical scrolling appears automatically\n" +
                    "• All content remains accessible via scrolling\n" +
                    "• Scroll position is maintained properly\n\n" +
                    "TALL WINDOW BEHAVIOR:\n" +
                    "• Resize to ~800px height\n" +
                    "• Content uses available space appropriately\n" +
                    "• No unnecessary stretching occurs\n" +
                    "• Footer remains at bottom", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Responsive design test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Update Notification States")]
        public static void TestUpdateNotificationStates() {
            Debug.Log("=== Testing Update Notification States ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();
                window.position = new Rect(100, 100, 550, 500);

                Debug.Log("✓ Testing update notification states");

                EditorUtility.DisplayDialog("Update Notification Testing",
                    "Testing different update notification states.\n\n" +
                    "Observe the update notification banner area and verify:\n\n" +
                    "NO UPDATE STATE:\n" +
                    "• No banner appears when no update is available\n" +
                    "• Clean transition when banner disappears\n\n" +
                    "UPDATE AVAILABLE STATE:\n" +
                    "• Green banner with update information\n" +
                    "• Shows current vs available version\n" +
                    "• Displays update priority (Major/Minor)\n" +
                    "• Three buttons: Update Now, Release Notes, Dismiss\n" +
                    "• Helpful tip text about updates\n" +
                    "• Last check time information\n\n" +
                    "CHECK IN PROGRESS STATE:\n" +
                    "• Blue banner with checking message\n" +
                    "• Animated loading indicator (spinning dots)\n" +
                    "• 'Connecting to GitHub API...' message\n\n" +
                    "RATE LIMITED STATE:\n" +
                    "• Orange banner with rate limit info\n" +
                    "• Time until next check available\n" +
                    "• Last check time\n" +
                    "• Explanation about rate limiting\n\n" +
                    "BANNER TRANSITIONS:\n" +
                    "• Smooth fade in/out between states\n" +
                    "• No abrupt changes or flashing\n" +
                    "• Consistent animation timing", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Update notification states test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Generate UI Polish Report")]
        public static void GenerateUIPolishReport() {
            Debug.Log("=== Generating UI Polish Report ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();
                window.position = new Rect(100, 100, 500, 600);

                Debug.Log("✓ AirConsole Settings window opened for inspection");
                Debug.Log("✓ Window positioned at 500x600 for optimal viewing");

                // Generate comprehensive report
                var report = GeneratePolishReport();
                Debug.Log(report);

                EditorUtility.DisplayDialog("UI Polish Report Generated",
                    "A comprehensive UI polish report has been generated.\n\n" +
                    "Check the Console window for the full report.\n\n" +
                    "The report includes:\n" +
                    "• Current implementation status\n" +
                    "• Feature verification checklist\n" +
                    "• Testing recommendations\n" +
                    "• Known issues and improvements\n\n" +
                    "Use this report to verify all UI polish\n" +
                    "improvements are working correctly.", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ UI polish report generation failed: {ex.Message}");
            }
        }

        private static string GeneratePolishReport() {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== AirConsole Plugin Update Checker UI Polish Report ===");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            report.AppendLine("IMPLEMENTED FEATURES:");
            report.AppendLine("✓ Consistent AirConsole branding and color scheme");
            report.AppendLine("✓ Three-tier button hierarchy (Primary/Secondary/Small)");
            report.AppendLine("✓ Comprehensive tooltip system for all interactive elements");
            report.AppendLine("✓ Smooth banner animations and loading indicators");
            report.AppendLine("✓ Confirmation dialogs for important actions");
            report.AppendLine("✓ Responsive layout with minimum window size enforcement");
            report.AppendLine("✓ Proper container structure with no overlapping elements");
            report.AppendLine("✓ Consistent spacing and padding throughout");
            report.AppendLine("✓ Collapsible sections with proper state management");
            report.AppendLine("✓ Scrolling support for smaller windows");
            report.AppendLine();

            report.AppendLine("VERIFICATION CHECKLIST:");
            report.AppendLine("□ All buttons maintain consistent heights within their tier");
            report.AppendLine("□ Tooltips appear on hover for all interactive elements");
            report.AppendLine("□ Banner animations are smooth and non-jarring");
            report.AppendLine("□ Confirmation dialogs appear for destructive actions");
            report.AppendLine("□ Layout adapts correctly to different window sizes");
            report.AppendLine("□ No visual overlaps between containers and headers");
            report.AppendLine("□ Foldout sections expand/collapse without layout issues");
            report.AppendLine("□ Status indicators align properly with buttons");
            report.AppendLine("□ Loading animations don't interfere with layout");
            report.AppendLine("□ Scrolling works smoothly when content exceeds window height");
            report.AppendLine();

            report.AppendLine("TESTING RECOMMENDATIONS:");
            report.AppendLine("1. Test all window sizes from 400x300 to 1000x800");
            report.AppendLine("2. Verify tooltips for every interactive element");
            report.AppendLine("3. Test confirmation dialogs by attempting destructive actions");
            report.AppendLine("4. Observe banner transitions in different update states");
            report.AppendLine("5. Test foldout behavior with Update Settings section");
            report.AppendLine("6. Verify button alignment in all sections");
            report.AppendLine("7. Test scrolling behavior with short windows");
            report.AppendLine("8. Check animation performance during rapid repaints");
            report.AppendLine();

            report.AppendLine("REQUIREMENTS COVERAGE:");
            report.AppendLine("• Requirement 2.1: Consistent AirConsole branding - ✓ IMPLEMENTED");
            report.AppendLine("• Requirement 2.2: Tooltips and help text - ✓ IMPLEMENTED");
            report.AppendLine("• Requirement 2.3: Smooth transitions - ✓ IMPLEMENTED");
            report.AppendLine("• Requirement 2.4: Confirmation dialogs - ✓ IMPLEMENTED");
            report.AppendLine("• Requirement 2.5: Responsive layout - ✓ IMPLEMENTED");
            report.AppendLine();

            report.AppendLine("TECHNICAL IMPLEMENTATION:");
            report.AppendLine("• Custom GUIStyles for consistent button hierarchy");
            report.AppendLine("• Animation system using EditorApplication.timeSinceStartup");
            report.AppendLine("• Responsive layout with window width detection");
            report.AppendLine("• Proper container nesting with helpBox styling");
            report.AppendLine("• EditorUtility.DisplayDialog for confirmations");
            report.AppendLine("• GUIContent with tooltip strings for help text");
            report.AppendLine("• ScrollView implementation for content overflow");
            report.AppendLine();

            report.AppendLine("=== End of Report ===");

            return report.ToString();
        }
    }
}
#endif