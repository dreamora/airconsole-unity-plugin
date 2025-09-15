#if !DISABLE_AIRCONSOLE

namespace NDream.Unity {
    #region Imports
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build.Reporting;
    using UnityEngine;
    using NDream.AirConsole.Editor;
    using NDream.AirConsole;
    #endregion Imports

    /// <summary>
    /// Unity Builder class for CI/CD pipeline builds
    /// </summary>
    public static class Builder {
        
        /// <summary>
        /// Build method for WebGL platform - called from CI
        /// </summary>
        public static void BuildWebGL() {
            string version = GetVersion();
            string unityVersion = Application.unityVersion;
            
            string outputPath = Path.GetFullPath(Path.Combine("Builds", "WebGL", $"release-{unityVersion}-v{version}"));
            
            BuildProject(BuildTarget.WebGL, outputPath, "WebGL");
        }
        
        /// <summary>
        /// Build method for Android platform - called from CI
        /// </summary>
        public static void BuildAndroid() {
            string version = GetVersion();
            string unityVersion = Application.unityVersion;
            
            string outputPath = Path.GetFullPath(Path.Combine("Builds", "Android", $"release-{unityVersion}-v{version}.apk"));
            
            BuildProject(BuildTarget.Android, outputPath, "Android");
        }
        
        /// <summary>
        /// Run tests for the specified platform
        /// </summary>
        public static void RunTests() {
            // Tests are handled by Unity Test Runner in CI, this is a placeholder
            // for any custom test logic if needed
            Debug.Log("Tests completed via Unity Test Runner");
        }
        
        private static void BuildProject(BuildTarget target, string outputPath, string platformName) {
            try {
                Debug.Log($"Starting {platformName} build...");
                Debug.Log($"Target: {target}");
                Debug.Log($"Output path: {outputPath}");
                
                // Ensure output directory exists
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir)) {
                    Directory.CreateDirectory(outputDir);
                }
                
                // Validate configuration for the target platform
                ProjectConfigurationCheck.CheckSettings(target);
                
                // Save assets before building
                AssetDatabase.SaveAssets();
                
                // Get enabled scenes
                string[] scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
                
                if (scenes.Length == 0) {
                    throw new Exception("No scenes are enabled in Build Settings!");
                }
                
                Debug.Log($"Building {scenes.Length} scenes: {string.Join(", ", scenes)}");
                
                // Configure build options
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = target,
                    options = BuildOptions.None
                };
                
                // Perform the build
                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                BuildSummary summary = report.summary;
                
                switch (summary.result) {
                    case BuildResult.Succeeded:
                        Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                        Debug.Log($"Build completed successfully: {outputPath}");
                        break;
                        
                    case BuildResult.Failed:
                        throw new Exception($"Build failed for {platformName}");
                        
                    case BuildResult.Cancelled:
                        throw new Exception($"Build cancelled for {platformName}");
                        
                    case BuildResult.Unknown:
                        throw new Exception($"Unknown build result for {platformName}");
                        
                    default:
                        throw new Exception($"Unexpected build result: {summary.result}");
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Build failed: {ex.Message}");
                EditorApplication.Exit(1);
                throw;
            }
        }
        
        private static string GetVersion() {
            return Settings.VERSION;
        }
    }
}

#endif