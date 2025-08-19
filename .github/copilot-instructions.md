# AirConsole Unity Plugin

The AirConsole Unity Plugin is a C# wrapper for the AirConsole JavaScript API, enabling local multiplayer realtime browser games. This Unity plugin supports WebGL and Android platforms and requires Unity 2022.3+ LTS.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Environment Setup
- **CRITICAL**: Install Unity 2022.3.62f1 or newer via Unity Hub from https://unity.com/download
- Download Unity Hub: `wget https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage`
- Make executable: `chmod +x UnityHub.AppImage`
- Install Unity 2022.3.62f1 with WebGL and Android build support modules
- **ALWAYS verify Unity version matches ProjectSettings/ProjectVersion.txt requirements**

### Project Structure
```
Assets/AirConsole/
├── scripts/Runtime/          # Core plugin runtime code
├── scripts/Editor/           # Unity Editor integration and build tools
├── scripts/Tests/            # NUnit tests (EditMode, PlayMode, Editor)
├── examples/                 # Sample scenes and code
├── WebGLTemplates/           # WebGL build templates
└── Documentation_1.7.pdf    # Complete documentation
```

### Build Process
- **NEVER CANCEL builds - Unity builds take 15-45 minutes. ALWAYS set timeout to 60+ minutes.**
- Open project in Unity: `Unity -projectPath .`
- Build via Unity menus: `Tools/AirConsole/Build/Web` or `Tools/AirConsole/Build/Android`
- **WebGL builds**: Output to `TestBuilds/Web/` directory
- **Android builds**: Output to `TestBuilds/Android/` as APK files
- Build validation runs automatically via `PreBuildProcessing.cs` and `PostBuildProcess.cs`

### Testing
- **NEVER CANCEL test runs - Unity tests take 5-15 minutes. Set timeout to 30+ minutes.**
- Run tests via Unity Test Runner: `Window > General > Test Runner`
- EditMode tests: Located in `Assets/AirConsole/scripts/Tests/EditMode/`
- PlayMode tests: Located in `Assets/AirConsole/scripts/Tests/PlayMode/`
- Editor tests: Located in `Assets/AirConsole/scripts/Tests/Editor/`
- Test timeout: 300 seconds per test (configured in `[Timeout(300)]` attributes)
- **CRITICAL**: Tests require NUnit framework and Newtonsoft.Json dependencies

### Development Tools
- Access via Unity menu: `Tools/AirConsole/Development/`
- Validate configurations:
  - `Tools/AirConsole/Development/Validate Web Configuration`
  - `Tools/AirConsole/Development/Validate Android Configuration`
- Build automation:
  - `Tools/AirConsole/Build/Web` - Builds WebGL version
  - `Tools/AirConsole/Build/Android` - Builds Android APK
  - `Tools/AirConsole/Build/Android Internal` - Builds internal Android version

### Build Time Expectations
- **WebGL Build**: 15-30 minutes (NEVER CANCEL - set 60+ minute timeout)
- **Android Build**: 20-45 minutes (NEVER CANCEL - set 60+ minute timeout)
- **Test Execution**: 5-15 minutes total (NEVER CANCEL - set 30+ minute timeout)
- **Project Import**: 2-5 minutes for initial setup

## Validation

### Pre-Build Validation
- **ALWAYS run configuration validation before building**:
  ```
  Unity -batchmode -quit -projectPath . -executeMethod NDream.AirConsole.Editor.DevelopmentTools.ValidateWebConfigurationMenuAction
  Unity -batchmode -quit -projectPath . -executeMethod NDream.AirConsole.Editor.DevelopmentTools.ValidateAndroidConfigurationMenuAction
  ```
- Verify Unity version compatibility: Check `Assets/AirConsole/scripts/SupportCheck/UnityVersionNotSupported.cs`
- Ensure required build modules are installed (WebGL, Android)

### Required Validation Steps
- **ALWAYS validate WebGL template setup**: Check `Assets/WebGLTemplates/` contains required files
- **ALWAYS verify controller.html exists** in WebGL template directory
- **ALWAYS check airconsole-unity-plugin.js** is present in template
- **ALWAYS validate AirConsole API version** - must use versioned API, not `airconsole-latest.js`

### Manual Testing
- **WebGL**: Test in browser with AirConsole controller interface
- **Android**: Deploy APK to Android TV device for testing
- **ALWAYS test safe area functionality** on Android devices
- **ALWAYS verify controller/screen communication** works correctly

## Common Tasks

### Building for Release
1. **ALWAYS validate configuration first**:
   - Run `Tools/AirConsole/Development/Validate Web Configuration`
   - Run `Tools/AirConsole/Development/Validate Android Configuration`
2. **Build WebGL** (25 minutes - NEVER CANCEL):
   - `Tools/AirConsole/Build/Web`
   - Output: `TestBuilds/Web/{timestamp}-{bundleId}-{commit}/`
3. **Build Android** (35 minutes - NEVER CANCEL):
   - `Tools/AirConsole/Build/Android`
   - Output: `TestBuilds/Android/{timestamp}-{bundleId}-{commit}.apk`

### Running Tests
1. **EditMode Tests** (5 minutes - set 30+ minute timeout):
   - Open `Window > General > Test Runner`
   - Select `EditMode` tab
   - Click "Run All"
2. **PlayMode Tests** (10 minutes - set 30+ minute timeout):
   - Select `PlayMode` tab
   - Click "Run All"
3. **Command line testing**:
   ```bash
   Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml
   Unity -batchmode -quit -projectPath . -runTests -testPlatform PlayMode -testResults TestResults-PlayMode.xml
   ```

### Plugin Development
- **ALWAYS unlock assemblies** if assembly reload is stuck: `Tools/AirConsole/Development/Unlock Assemblies`
- **ALWAYS check git status** before building - builds auto-commit changes
- **Build automation runs git commands** - ensure clean working directory
- **Version updates**: Edit `Assets/AirConsole/scripts/Runtime/Settings.cs` VERSION constant

### Key Files to Monitor
When making changes, **ALWAYS check these files**:
- `Assets/AirConsole/scripts/Runtime/Settings.cs` - Version and configuration
- `Assets/AirConsole/scripts/Editor/BuildAutomation/PreBuildProcessing.cs` - Build validation
- `Assets/AirConsole/scripts/Editor/BuildAutomation/PostBuildProcess.cs` - Post-build validation
- `Assets/WebGLTemplates/AirConsole-U6/` - WebGL template files

### Documentation References
```
ls -la Assets/AirConsole/
Documentation_1.7.pdf          # Complete plugin documentation
Upgrade_Plugin_Version.md      # Version upgrade instructions
README.txt                     # Basic setup information
CHANGELOG.md                   # Version history and changes
```

### Assembly Definitions
```
find Assets -name "*.asmdef"
Assets/AirConsole/examples/AirConsole.Examples.asmdef
Assets/AirConsole/scripts/Runtime/AirConsole.Runtime.asmdef
Assets/AirConsole/scripts/Editor/AirConsole.Editor.asmdef
Assets/AirConsole/scripts/SupportCheck/AirConsole.SupportCheck.asmdef
Assets/AirConsole/scripts/Tests/EditMode/AirConsole.EditMode.Tests.asmdef
Assets/AirConsole/scripts/Tests/PlayMode/AirConsole.PlayMode.Tests.asmdef
Assets/AirConsole/scripts/Tests/Editor/AirConsole.Editor.Tests.asmdef
```

### Package Dependencies
```
cat Packages/manifest.json
Key dependencies:
- com.airconsole.unity-webview (v1.1.5)
- com.unity.test-framework (v1.4.4)
- com.unity.ext.nunit (v2.0.5)
- Unity modules for WebGL and Android platforms
```

### CI/CD Information
- GitHub Actions workflow: `.github/workflows/create-release.yaml`
- Uses Unity version: 2022.3.62f1
- Build platforms: WebGL, Android
- **CRITICAL**: CI builds are currently commented out - manual builds required
- License activation: `.github/workflows/activation.yaml`

## CRITICAL REMINDERS
- **NEVER CANCEL Unity builds or tests** - they legitimately take 15-45 minutes
- **ALWAYS set 60+ minute timeouts** for build commands
- **ALWAYS set 30+ minute timeouts** for test commands
- **Unity version 2022.3+ is mandatory** - older versions will fail with build errors
- **WebGL and Android modules must be installed** in Unity Editor
- **ALWAYS validate configuration before building** using the development tools menu