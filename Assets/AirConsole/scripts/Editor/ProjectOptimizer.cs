#if !DISABLE_AIRCONSOLE
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace NDream.AirConsole.Editor {
    /// <summary>
    /// AirConsole Project Optimizer window that provides visual recommendations 
    /// for optimizing Unity projects for AirConsole web and Android platforms.
    /// 
    /// This tool is based on the recommendations from ProjectConfigurationCheck 
    /// but presents them in a user-friendly interface with individual apply buttons.
    /// </summary>
    public class ProjectOptimizer : EditorWindow {
        private VisualTreeAsset uiDocument;
        private StyleSheet styleSheet;
        
        [MenuItem("Window/AirConsole/Project Optimizer")]
        public static void ShowWindow() {
            ProjectOptimizer wnd = GetWindow<ProjectOptimizer>();
            wnd.titleContent = new GUIContent("Project Optimizer");
            wnd.minSize = new Vector2(400, 600);
        }

        public void CreateGUI() {
            // Load UXML and USS files
            uiDocument = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AirConsole/scripts/Editor/ProjectOptimizer.uxml");
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AirConsole/scripts/Editor/ProjectOptimizer.uss");
            
            if (uiDocument == null) {
                Debug.LogError("Could not load ProjectOptimizer.uxml file");
                return;
            }
            
            // Clone the UXML tree
            VisualElement root = rootVisualElement;
            uiDocument.CloneTree(root);
            
            // Apply the stylesheet
            if (styleSheet != null) {
                root.styleSheets.Add(styleSheet);
            }
            
            // Setup event handlers
            SetupEventHandlers();
            
            // Update the UI with current settings
            RefreshRecommendations();
        }

        private void SetupEventHandlers() {
            VisualElement root = rootVisualElement;
            
            // Apply All button
            var applyAllButton = root.Q<Button>("apply-all-button");
            if (applyAllButton != null) {
                applyAllButton.clicked += ApplyAllRecommendations;
            }
            
            // Refresh button
            var refreshButton = root.Q<Button>("refresh-button");
            if (refreshButton != null) {
                refreshButton.clicked += RefreshRecommendations;
            }
            
            // Setup individual apply buttons for each recommendation
            SetupRecommendationButton("data-caching", () => PlayerSettings.WebGL.dataCaching, 
                () => PlayerSettings.WebGL.dataCaching = false);
            
            SetupRecommendationButton("memory-growth-mode", () => PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None,
                () => {
                    PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.None;
                    PlayerSettings.WebGL.initialMemorySize = Mathf.Min(512,
                        Mathf.Max(PlayerSettings.WebGL.initialMemorySize, PlayerSettings.WebGL.maximumMemorySize));
                });
            
            SetupRecommendationButton("initial-memory-size", () => PlayerSettings.WebGL.memorySize > 512,
                () => PlayerSettings.WebGL.initialMemorySize = 512);
            
            SetupRecommendationButton("name-files-as-hashes", () => PlayerSettings.WebGL.nameFilesAsHashes,
                () => PlayerSettings.WebGL.nameFilesAsHashes = false);
            
            SetupRecommendationButton("webgl-scripting-backend", () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL) != ScriptingImplementation.IL2CPP,
                () => PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP));
            
            SetupRecommendationButton("multithreaded-rendering", () => !PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android),
                () => PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true));
            
            SetupRecommendationButton("preferred-install-location", () => PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto,
                () => PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto);
            
            SetupRecommendationButton("vulkan-swapchain-buffers", () => PlayerSettings.vulkanNumSwapchainBuffers < 3,
                () => PlayerSettings.vulkanNumSwapchainBuffers = 3);
            
            SetupRecommendationButton("optimized-frame-pacing", () => !PlayerSettings.Android.optimizedFramePacing,
                () => PlayerSettings.Android.optimizedFramePacing = true);
            
            SetupRecommendationButton("target-architecture", () => PlayerSettings.Android.targetArchitectures != (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7),
                () => PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7);
            
            SetupRecommendationButton("android-scripting-backend", () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP,
                () => PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP));
            
            SetupRecommendationButton("target-sdk-version", () => (int)PlayerSettings.Android.targetSdkVersion < 34,
                () => PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34);
            
            SetupRecommendationButton("run-in-background", () => {
                bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                return PlayerSettings.runInBackground != shouldRunInBackground;
            }, () => {
                bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                PlayerSettings.runInBackground = shouldRunInBackground;
            });
            
            SetupRecommendationButton("mute-other-audio-sources", () => PlayerSettings.muteOtherAudioSources,
                () => PlayerSettings.muteOtherAudioSources = false);
            
            SetupRecommendationButton("allow-unsafe-code", () => PlayerSettings.allowUnsafeCode,
                () => PlayerSettings.allowUnsafeCode = false);
            
            SetupRecommendationButton("reset-resolution-on-window-resize", () => !PlayerSettings.resetResolutionOnWindowResize,
                () => PlayerSettings.resetResolutionOnWindowResize = true);
            
            SetupRecommendationButton("unity-logo-splash-screen", () => PlayerSettings.SplashScreen.showUnityLogo,
                () => PlayerSettings.SplashScreen.showUnityLogo = false);
        }

        private void SetupRecommendationButton(string elementName, System.Func<bool> needsAttention, System.Action applyFix) {
            VisualElement root = rootVisualElement;
            var container = root.Q<VisualElement>(elementName);
            if (container == null) return;
            
            var button = container.Q<Button>(className: "apply-button");
            if (button != null) {
                button.clicked += () => {
                    try {
                        applyFix();
                        UpdateRecommendationStatus(elementName, needsAttention);
                    } catch (System.Exception ex) {
                        Debug.LogError($"Failed to apply recommendation '{elementName}': {ex.Message}");
                    }
                };
            }
        }

        private void UpdateRecommendationStatus(string elementName, System.Func<bool> needsAttention) {
            VisualElement root = rootVisualElement;
            var container = root.Q<VisualElement>(elementName);
            if (container == null) return;
            
            var statusIndicator = container.Q<VisualElement>(className: "status-indicator");
            var applyButton = container.Q<Button>(className: "apply-button");
            var statusCheckmark = container.Q<Label>(className: "status-checkmark");
            
            bool needsFix = false;
            try {
                needsFix = needsAttention();
            } catch (System.Exception) {
                needsFix = true;
            }
            
            if (needsFix) {
                statusIndicator?.RemoveFromClassList("compliant");
                applyButton?.RemoveFromClassList("hidden");
                statusCheckmark?.AddToClassList("hidden");
            } else {
                statusIndicator?.AddToClassList("compliant");
                applyButton?.AddToClassList("hidden");
                statusCheckmark?.RemoveFromClassList("hidden");
            }
        }

        private void RefreshRecommendations() {
            VisualElement root = rootVisualElement;
            
            // Update all recommendation statuses
            UpdateRecommendationStatus("data-caching", () => PlayerSettings.WebGL.dataCaching);
            UpdateRecommendationStatus("memory-growth-mode", () => PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None);
            UpdateRecommendationStatus("initial-memory-size", () => PlayerSettings.WebGL.memorySize > 512);
            UpdateRecommendationStatus("name-files-as-hashes", () => PlayerSettings.WebGL.nameFilesAsHashes);
            UpdateRecommendationStatus("webgl-scripting-backend", () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL) != ScriptingImplementation.IL2CPP);
            
            UpdateRecommendationStatus("multithreaded-rendering", () => !PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android));
            UpdateRecommendationStatus("preferred-install-location", () => PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto);
            UpdateRecommendationStatus("vulkan-swapchain-buffers", () => PlayerSettings.vulkanNumSwapchainBuffers < 3);
            UpdateRecommendationStatus("optimized-frame-pacing", () => !PlayerSettings.Android.optimizedFramePacing);
            UpdateRecommendationStatus("target-architecture", () => PlayerSettings.Android.targetArchitectures != (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7));
            UpdateRecommendationStatus("android-scripting-backend", () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP);
            UpdateRecommendationStatus("target-sdk-version", () => (int)PlayerSettings.Android.targetSdkVersion < 34);
            
            UpdateRecommendationStatus("run-in-background", () => {
                bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                return PlayerSettings.runInBackground != shouldRunInBackground;
            });
            UpdateRecommendationStatus("mute-other-audio-sources", () => PlayerSettings.muteOtherAudioSources);
            UpdateRecommendationStatus("allow-unsafe-code", () => PlayerSettings.allowUnsafeCode);
            UpdateRecommendationStatus("reset-resolution-on-window-resize", () => !PlayerSettings.resetResolutionOnWindowResize);
            UpdateRecommendationStatus("unity-logo-splash-screen", () => PlayerSettings.SplashScreen.showUnityLogo);
            
            // Update the run-in-background description text based on current build target
            var runInBackgroundContainer = root.Q<VisualElement>("run-in-background");
            if (runInBackgroundContainer != null) {
                var descLabel = runInBackgroundContainer.Q<Label>(className: "recommendation-description");
                if (descLabel != null) {
                    bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                    descLabel.text = $"Should be {shouldRunInBackground} for {EditorUserBuildSettings.activeBuildTarget}";
                }
            }
        }

        private void ApplyAllRecommendations() {
            int applied = 0;
            int errors = 0;
            
            try {
                // Apply all WebGL recommendations
                if (PlayerSettings.WebGL.dataCaching) {
                    PlayerSettings.WebGL.dataCaching = false;
                    applied++;
                }
                
                if (PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None) {
                    PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.None;
                    PlayerSettings.WebGL.initialMemorySize = Mathf.Min(512,
                        Mathf.Max(PlayerSettings.WebGL.initialMemorySize, PlayerSettings.WebGL.maximumMemorySize));
                    applied++;
                }
                
                if (PlayerSettings.WebGL.memorySize > 512) {
                    PlayerSettings.WebGL.initialMemorySize = 512;
                    applied++;
                }

                if (PlayerSettings.WebGL.nameFilesAsHashes) {
                    PlayerSettings.WebGL.nameFilesAsHashes = false;
                    applied++;
                }

                if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL) != ScriptingImplementation.IL2CPP) {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
                    applied++;
                }
            } catch (System.Exception ex) {
                Debug.LogError($"Error applying WebGL recommendations: {ex.Message}");
                errors++;
            }

            try {
                // Apply all Android recommendations
                if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android)) {
                    PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                    applied++;
                }
                
                if (PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto) {
                    PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
                    applied++;
                }
                
                if (PlayerSettings.vulkanNumSwapchainBuffers < 3) {
                    PlayerSettings.vulkanNumSwapchainBuffers = 3;
                    applied++;
                }
                
                if (!PlayerSettings.Android.optimizedFramePacing) {
                    PlayerSettings.Android.optimizedFramePacing = true;
                    applied++;
                }

                if (PlayerSettings.Android.targetArchitectures != (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7)) {
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                    applied++;
                }

                if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP) {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    applied++;
                }

                if ((int)PlayerSettings.Android.targetSdkVersion < 34) {
                    PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
                    applied++;
                }
            } catch (System.Exception ex) {
                Debug.LogError($"Error applying Android recommendations: {ex.Message}");
                errors++;
            }

            try {
                // Apply all General recommendations
                bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                if (PlayerSettings.runInBackground != shouldRunInBackground) {
                    PlayerSettings.runInBackground = shouldRunInBackground;
                    applied++;
                }
                
                if (PlayerSettings.muteOtherAudioSources) {
                    PlayerSettings.muteOtherAudioSources = false;
                    applied++;
                }

                if (PlayerSettings.allowUnsafeCode) {
                    PlayerSettings.allowUnsafeCode = false;
                    applied++;
                }

                if (!PlayerSettings.resetResolutionOnWindowResize) {
                    PlayerSettings.resetResolutionOnWindowResize = true;
                    applied++;
                }

                if (PlayerSettings.SplashScreen.showUnityLogo) {
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                    applied++;
                }
            } catch (System.Exception ex) {
                Debug.LogError($"Error applying General recommendations: {ex.Message}");
                errors++;
            }

            // Refresh the view to show updated status
            RefreshRecommendations();
            
            // Show a notification
            if (errors == 0) {
                Debug.Log($"AirConsole Project Optimizer: {applied} recommendations have been applied successfully!");
            } else {
                Debug.LogWarning($"AirConsole Project Optimizer: {applied} recommendations applied, {errors} errors occurred. Check console for details.");
            }
        }
    }
}
#endif