#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Manual validation script for container and button layout fixes
    /// </summary>
    public static class ContainerLayoutValidation {

        [MenuItem("AirConsole/Tests/Validate Container Layout")]
        public static void ValidateContainerLayout() {
            Debug.Log("=== Container Layout Validation ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();

                Debug.Log("✓ Settings window opened for container layout validation");

                EditorUtility.DisplayDialog("Container Layout Validation",
                    "Settings window is now open for container layout validation.\n\n" +
                    "Please verify:\n" +
                    "• Section headers are inside their containers\n" +
                    "• No overlapping between titles and containers\n" +
                    "• Proper spacing between sections\n" +
                    "• Button rows are properly aligned\n" +
                    "• Containers have consistent padding\n" +
                    "• Foldout sections work correctly\n" +
                    "• Footer button is properly positioned", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Container layout validation failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Button Row Consistency")]
        public static void TestButtonRowConsistency() {
            Debug.Log("=== Button Row Consistency Test ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();

                // Test different window sizes to check button row behavior
                window.position = new Rect(100, 100, 350, 500); // Narrow
                Debug.Log("✓ Testing narrow layout (350px width)");

                EditorApplication.delayCall += () => {
                    window.position = new Rect(100, 100, 500, 500); // Medium
                    Debug.Log("✓ Testing medium layout (500px width)");

                    EditorApplication.delayCall += () => {
                        window.position = new Rect(100, 100, 700, 500); // Wide
                        Debug.Log("✓ Testing wide layout (700px width)");
                    };
                };

                EditorUtility.DisplayDialog("Button Row Testing",
                    "Testing button rows at different window sizes.\n\n" +
                    "Please verify:\n" +
                    "• All button rows maintain proper alignment\n" +
                    "• Buttons don't overlap or get cut off\n" +
                    "• FlexibleSpace works correctly\n" +
                    "• Button spacing is consistent\n" +
                    "• Status indicators align properly\n" +
                    "• Loading indicators don't interfere with buttons", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Button row consistency test failed: {ex.Message}");
            }
        }

        [MenuItem("AirConsole/Tests/Test Section Container Structure")]
        public static void TestSectionContainerStructure() {
            Debug.Log("=== Section Container Structure Test ===");

            try {
                var window = EditorWindow.GetWindow<SettingWindow>();
                window.Show();
                window.Focus();

                Debug.Log("✓ Testing section container structure");
                Debug.Log("Manual test: Verify each section has proper container boundaries");
                Debug.Log("Manual test: Check that headers don't overlap with container edges");
                Debug.Log("Manual test: Ensure consistent padding within containers");
                Debug.Log("Manual test: Verify foldout sections expand/collapse correctly");

                EditorUtility.DisplayDialog("Section Container Testing",
                    "Testing section container structure.\n\n" +
                    "Please verify:\n" +
                    "• Update Settings: Header inside container, proper foldout\n" +
                    "• Connection Settings: Header inside container, proper spacing\n" +
                    "• Debug Settings: Header inside container, toggle group works\n" +
                    "• Footer: Reset button properly positioned\n" +
                    "• All containers have consistent visual appearance\n" +
                    "• No visual overlaps or gaps between sections", "OK");

            } catch (Exception ex) {
                Debug.LogError($"✗ Section container structure test failed: {ex.Message}");
            }
        }
    }
}
#endif