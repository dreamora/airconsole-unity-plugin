#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NDream.AirConsole.Editor.Tests {
    public class ProjectOptimizerTests {
        
        [Test]
        public void ProjectOptimizer_CanCreateWindow() {
            // Test that the window can be created without errors
            var window = EditorWindow.GetWindow<ProjectOptimizer>();
            Assert.IsNotNull(window);
            
            // Test that the window has the correct title
            Assert.AreEqual("Project Optimizer", window.titleContent.text);
            
            // Clean up
            if (window != null) {
                window.Close();
            }
        }

        [Test]
        public void ProjectOptimizer_HasMenuItemAttribute() {
            // Test that the ShowWindow method has the correct MenuItem attribute
            var method = typeof(ProjectOptimizer).GetMethod("ShowWindow");
            Assert.IsNotNull(method);
            
            var menuItemAttribute = method.GetCustomAttributes(typeof(MenuItemAttribute), false);
            Assert.IsNotEmpty(menuItemAttribute);
            
            var menuItem = (MenuItemAttribute)menuItemAttribute[0];
            Assert.AreEqual("Window/AirConsole/Project Optimizer", menuItem.menuItem);
        }

        [Test]
        public void ProjectOptimizer_CreateGUI_DoesNotThrowException() {
            // Test that CreateGUI method can be called without throwing exceptions
            var window = EditorWindow.GetWindow<ProjectOptimizer>();
            
            Assert.DoesNotThrow(() => {
                window.CreateGUI();
            });
            
            // Clean up
            if (window != null) {
                window.Close();
            }
        }

        [Test]
        public void ProjectOptimizer_UsesUXMLAndUSS() {
            // Test that the UXML and USS files can be loaded
            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(
                "Assets/AirConsole/scripts/Editor/ProjectOptimizer.uxml");
            var uss = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.StyleSheet>(
                "Assets/AirConsole/scripts/Editor/ProjectOptimizer.uss");
            
            Assert.IsNotNull(uxml, "UXML file should be loadable");
            Assert.IsNotNull(uss, "USS file should be loadable");
        }
    }
}
#endif