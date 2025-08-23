#if !DISABLE_AIRCONSOLE
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

namespace NDream.AirConsole.Editor {
    /// <summary>
    /// AirConsole Project Optimizer window that provides visual recommendations 
    /// for optimizing Unity projects for AirConsole web and Android platforms.
    /// 
    /// This tool is based on the recommendations from ProjectConfigurationCheck 
    /// but presents them in a user-friendly interface with individual apply buttons.
    /// </summary>
    public class ProjectOptimizer : EditorWindow {
        private ScrollView mainScrollView;
        
        [MenuItem("Window/AirConsole/Project Optimizer")]
        public static void ShowWindow() {
            ProjectOptimizer wnd = GetWindow<ProjectOptimizer>();
            wnd.titleContent = new GUIContent("Project Optimizer");
            wnd.minSize = new Vector2(400, 600);
        }

        public void CreateGUI() {
            VisualElement root = rootVisualElement;
            
            // Create main scroll view
            mainScrollView = new ScrollView();
            root.Add(mainScrollView);
            
            // Add header
            mainScrollView.Add(CreateHeader("AirConsole Project Optimizer", false));
            mainScrollView.Add(CreateDescription("Optimize your project for AirConsole web and Android platforms"));
            
            // Add Apply All button at the top
            var applyAllButton = new Button(ApplyAllRecommendations) {
                text = "Apply All Recommendations",
                style = { 
                    marginBottom = 20,
                    backgroundColor = new Color(0.2f, 0.6f, 0.2f),
                    color = Color.white,
                    height = 30
                }
            };
            mainScrollView.Add(applyAllButton);
            
            // Add platform sections
            CreateWebGLSection();
            CreateAndroidSection();
            CreateGeneralSection();
            
            // Refresh button
            var refreshButton = new Button(RefreshRecommendations) {
                text = "Refresh Recommendations",
                style = { marginTop = 20 }
            };
            mainScrollView.Add(refreshButton);
        }

        private void CreateWebGLSection() {
            mainScrollView.Add(CreateHeader("WebGL Recommendations"));
            
            // Data Caching
            CreateRecommendationItem(
                "Data Caching",
                "Should be disabled to avoid interference with automotive requirements",
                () => PlayerSettings.WebGL.dataCaching,
                () => PlayerSettings.WebGL.dataCaching = false,
                false
            );
            
            // Memory Growth Mode
            CreateRecommendationItem(
                "Memory Growth Mode",
                "Should be set to None for performance and stability on automotive",
                () => PlayerSettings.WebGL.memoryGrowthMode != WebGLMemoryGrowthMode.None,
                () => {
                    PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.None;
                    PlayerSettings.WebGL.initialMemorySize = Mathf.Min(512,
                        Mathf.Max(PlayerSettings.WebGL.initialMemorySize, PlayerSettings.WebGL.maximumMemorySize));
                },
                false
            );
            
            // Initial Memory Size
            CreateRecommendationItem(
                "Initial Memory Size",
                "Should stay at or below 512MB for automotive compatibility",
                () => PlayerSettings.WebGL.memorySize > 512,
                () => PlayerSettings.WebGL.initialMemorySize = 512,
                false
            );

            // Name Files As Hashes
            CreateRecommendationItem(
                "Name Files As Hashes",
                "Should be disabled as we upload into timestamp based folders",
                () => PlayerSettings.WebGL.nameFilesAsHashes,
                () => PlayerSettings.WebGL.nameFilesAsHashes = false,
                false
            );

            // Scripting Backend
            CreateRecommendationItem(
                "Scripting Backend",
                "Should be set to IL2CPP for WebGL",
                () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL) != ScriptingImplementation.IL2CPP,
                () => PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP),
                false
            );
        }

        private void CreateAndroidSection() {
            mainScrollView.Add(CreateHeader("Android Recommendations"));
            
            // Multithreaded Rendering
            CreateRecommendationItem(
                "Multithreaded Rendering",
                "Ensures optimal performance and thermal load",
                () => !PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android),
                () => PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true),
                true
            );
            
            // Preferred Install Location
            CreateRecommendationItem(
                "Preferred Install Location",
                "Should be set to Auto for best compatibility",
                () => PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto,
                () => PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto,
                false
            );
            
            // Vulkan Swapchain Buffers
            CreateRecommendationItem(
                "Vulkan Swapchain Buffers",
                "Must contain at least 3 buffers for proper rendering",
                () => PlayerSettings.vulkanNumSwapchainBuffers < 3,
                () => PlayerSettings.vulkanNumSwapchainBuffers = 3,
                false
            );
            
            // Optimized Frame Pacing
            CreateRecommendationItem(
                "Optimized Frame Pacing",
                "Improves frame consistency and performance on Android",
                () => !PlayerSettings.Android.optimizedFramePacing,
                () => PlayerSettings.Android.optimizedFramePacing = true,
                false
            );

            // Target Architecture
            CreateRecommendationItem(
                "Target Architecture",
                "Should include both ARM64 and ARMv7 for compatibility",
                () => PlayerSettings.Android.targetArchitectures != (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7),
                () => PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7,
                false
            );

            // Scripting Backend
            CreateRecommendationItem(
                "Scripting Backend",
                "Should be set to IL2CPP for Android",
                () => PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP,
                () => PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP),
                false
            );

            // Target SDK Version
            CreateRecommendationItem(
                "Target SDK Version",
                "Should be at least 34 for Google Play compatibility",
                () => (int)PlayerSettings.Android.targetSdkVersion < 34,
                () => PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34,
                false
            );
        }

        private void CreateGeneralSection() {
            mainScrollView.Add(CreateHeader("General Recommendations"));
            
            // Run In Background
            bool shouldRunInBackground = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            CreateRecommendationItem(
                "Run In Background",
                $"Should be {shouldRunInBackground} for {EditorUserBuildSettings.activeBuildTarget}",
                () => PlayerSettings.runInBackground != shouldRunInBackground,
                () => PlayerSettings.runInBackground = shouldRunInBackground,
                false
            );
            
            // Mute Other Audio Sources
            CreateRecommendationItem(
                "Mute Other Audio Sources",
                "Should be disabled for automotive compatibility",
                () => PlayerSettings.muteOtherAudioSources,
                () => PlayerSettings.muteOtherAudioSources = false,
                false
            );

            // Unsafe Code
            CreateRecommendationItem(
                "Allow Unsafe Code",
                "Should be disabled to ensure automotive platform compatibility",
                () => PlayerSettings.allowUnsafeCode,
                () => PlayerSettings.allowUnsafeCode = false,
                false
            );

            // Reset Resolution On Window Resize
            CreateRecommendationItem(
                "Reset Resolution On Window Resize",
                "Should be enabled for proper window handling",
                () => !PlayerSettings.resetResolutionOnWindowResize,
                () => PlayerSettings.resetResolutionOnWindowResize = true,
                false
            );

            // Unity Logo in Splash Screen
            CreateRecommendationItem(
                "Unity Logo in Splash Screen",
                "Should be disabled for cleaner presentation",
                () => PlayerSettings.SplashScreen.showUnityLogo,
                () => PlayerSettings.SplashScreen.showUnityLogo = false,
                false
            );
        }

        private void CreateRecommendationItem(string title, string description, System.Func<bool> needsAttention, System.Action applyFix, bool isOptimal) {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginBottom = 10;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.borderBottomWidth = 1;
            container.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            
            // Status indicator
            var statusIndicator = new VisualElement();
            statusIndicator.style.width = 16;
            statusIndicator.style.height = 16;
            statusIndicator.style.borderRadius = 8;
            statusIndicator.style.marginRight = 10;
            
            bool needsFix = false;
            try {
                needsFix = needsAttention();
            } catch (System.Exception) {
                // If we can't determine the status, assume it needs attention
                needsFix = true;
            }
            
            if (needsFix) {
                statusIndicator.style.backgroundColor = new Color(1f, 0.6f, 0f); // Orange for needs attention
            } else {
                statusIndicator.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Green for good
            }
            
            container.Add(statusIndicator);
            
            // Content container
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            
            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14;
            contentContainer.Add(titleLabel);
            
            var descLabel = new Label(description);
            descLabel.style.fontSize = 12;
            descLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            contentContainer.Add(descLabel);
            
            container.Add(contentContainer);
            
            // Apply button
            if (needsFix) {
                var applyButton = new Button(() => {
                    try {
                        applyFix();
                        RefreshRecommendations();
                    } catch (System.Exception ex) {
                        Debug.LogError($"Failed to apply recommendation '{title}': {ex.Message}");
                    }
                }) {
                    text = "Apply",
                    style = { 
                        marginLeft = 10,
                        minWidth = 60
                    }
                };
                container.Add(applyButton);
            } else {
                var statusLabel = new Label("✓");
                statusLabel.style.color = new Color(0.2f, 0.8f, 0.2f);
                statusLabel.style.fontSize = 16;
                statusLabel.style.marginLeft = 10;
                statusLabel.style.minWidth = 60;
                statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                container.Add(statusLabel);
            }
            
            mainScrollView.Add(container);
        }

        private Label CreateHeader(string text, bool hasTopMargin = true) {
            Label label = new Label(text);
            if (hasTopMargin) {
                label.style.marginTop = 20;
            }
            label.style.marginBottom = 10;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 16;
            return label;
        }

        private Label CreateDescription(string text) {
            Label label = new Label(text);
            label.style.fontSize = 12;
            label.style.color = new Color(0.7f, 0.7f, 0.7f);
            label.style.marginBottom = 20;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private void RefreshRecommendations() {
            // Clear existing content
            mainScrollView.Clear();
            
            // Recreate content
            mainScrollView.Add(CreateHeader("AirConsole Project Optimizer", false));
            mainScrollView.Add(CreateDescription("Optimize your project for AirConsole web and Android platforms"));
            
            // Add Apply All button at the top
            var applyAllButton = new Button(ApplyAllRecommendations) {
                text = "Apply All Recommendations",
                style = { 
                    marginBottom = 20,
                    backgroundColor = new Color(0.2f, 0.6f, 0.2f),
                    color = Color.white,
                    height = 30
                }
            };
            mainScrollView.Add(applyAllButton);
            
            CreateWebGLSection();
            CreateAndroidSection();
            CreateGeneralSection();
            
            // Refresh button
            var refreshButton = new Button(RefreshRecommendations) {
                text = "Refresh Recommendations",
                style = { marginTop = 20 }
            };
            mainScrollView.Add(refreshButton);
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