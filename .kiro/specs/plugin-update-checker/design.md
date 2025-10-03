# Design Document

## Overview

The plugin update checker feature will enhance the existing `GithubUpdate` class functionality by creating a dedicated Update Checker editor window and providing a user-friendly notification system. The design leverages the existing GitHub API integration while adding persistent settings, automatic triggering, and improved UI feedback in its own dedicated interface.

## Architecture

### Core Components

1. **UpdateChecker** - Enhanced wrapper around existing `GithubUpdate` functionality
2. **UpdateSettings** - Persistent configuration for update checking preferences
3. **UpdateCheckerWindow** - Dedicated editor window for update functionality and UI
4. **UpdateScheduler** - Handles automatic periodic checking and editor startup triggers

### Integration Points

- **UpdateCheckerWindow.cs** - Dedicated editor window for all update functionality
- **GithubUpdate.cs** - Existing GitHub API functionality (minimal modifications needed)
- **Settings.cs** - Extended to include update checker preferences
- **EditorApplication callbacks** - For startup triggers and periodic checks

## Components and Interfaces

### UpdateChecker Class

```csharp
public static class UpdateChecker
{
    // Public API
    public static bool IsUpdateAvailable { get; }
    public static Version LatestVersion { get; }
    public static bool IsCheckInProgress { get; }
    public static DateTime LastCheckTime { get; }

    // Methods
    public static void StartAutomaticChecking();
    public static void StopAutomaticChecking();
    public static void CheckForUpdatesAsync();
    public static void OpenReleasePage();
    public static void DismissCurrentUpdate();
}
```

### UpdateSettings ScriptableObject

```csharp
[CreateAssetMenu(fileName = "UpdateSettings", menuName = "AirConsole/Update Settings")]
public class UpdateSettings : ScriptableObject
{
    [SerializeField] private bool automaticCheckEnabled = true;
    [SerializeField] private int checkIntervalHours = 12; // Minimum 12 hours to prevent rate limiting
    [SerializeField] private string lastCheckTime = ""; // DateTime serialized as string
    [SerializeField] private string dismissedVersion = "";
    [SerializeField] private bool checkOnStartup = true;
    [SerializeField] private int failedCheckCount = 0; // Track consecutive failures for backoff

    // Properties with validation
    public bool AutomaticCheckEnabled
    {
        get => automaticCheckEnabled;
        set => automaticCheckEnabled = value;
    }

    public int CheckIntervalHours
    {
        get => Mathf.Max(12, checkIntervalHours); // Enforce minimum 12 hours
        set => checkIntervalHours = Mathf.Max(12, value);
    }

    public DateTime LastCheckTime
    {
        get => DateTime.TryParse(lastCheckTime, out var dt) ? dt : DateTime.MinValue;
        set => lastCheckTime = value.ToString("O"); // ISO 8601 format
    }

    public string DismissedVersion
    {
        get => dismissedVersion;
        set => dismissedVersion = value ?? "";
    }

    public bool CheckOnStartup
    {
        get => checkOnStartup;
        set => checkOnStartup = value;
    }

    public int FailedCheckCount
    {
        get => failedCheckCount;
        set => failedCheckCount = Mathf.Max(0, value);
    }

    // Helper methods
    public bool CanCheckNow()
    {
        return DateTime.Now - LastCheckTime >= TimeSpan.FromHours(CheckIntervalHours);
    }

    public TimeSpan TimeUntilNextCheck()
    {
        var elapsed = DateTime.Now - LastCheckTime;
        var interval = TimeSpan.FromHours(CheckIntervalHours);
        return elapsed >= interval ? TimeSpan.Zero : interval - elapsed;
    }
}
```

### UpdateCheckerWindow Component

Dedicated editor window with complete update functionality:

```csharp
public class UpdateCheckerWindow : EditorWindow
{
    [MenuItem("AirConsole/Update Checker")]
    public static void ShowWindow()
    {
        GetWindow<UpdateCheckerWindow>("AirConsole Update Checker");
    }

    private void OnGUI()
    {
        // Current version display
        // Latest version information
        // Update status and availability
        // Manual check button
        // Update settings and preferences
        // Dismiss functionality
        // Open release page button
    }
}
```

**Window Features:**
- Current version and latest available version display
- Update status indicator (available, up-to-date, checking)
- Manual "Check Now" button with rate limiting feedback
- Update settings (automatic checking, intervals)
- Dismiss current update functionality
- Direct link to GitHub releases page
- Last check time and next available check time

## Data Models

### Update Status Model

```csharp
public class UpdateStatus
{
    public bool IsAvailable { get; set; }
    public Version CurrentVersion { get; set; }
    public Version LatestVersion { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string ReleaseUrl { get; set; }
    public bool IsDismissed { get; set; }
}
```

### Settings Persistence

Update settings will be stored as a `ScriptableObject` asset for better persistence and serialization:

**Asset Location**: `Assets/AirConsole/Editor/UpdateSettings.asset`

**Benefits of ScriptableObject approach**:
- Automatic serialization and persistence
- Version control friendly (can be committed to repo)
- Inspector-friendly for debugging
- Type-safe property access with validation
- Built-in Unity asset management

**Fallback Strategy**: If the asset doesn't exist, create it with default values on first access. This ensures the system works out-of-the-box without manual setup.

## Error Handling

### Network Error Handling

1. **Connection Failures**: Gracefully handle network timeouts and connection errors
2. **API Rate Limiting**:
   - Enforce minimum 12-hour intervals between automatic checks
   - Detect rate limit responses (HTTP 403 with rate limit headers)
   - Implement exponential backoff for failed requests (1h, 4h, 12h, 24h)
   - Manual checks respect the same rate limiting rules
3. **Malformed Responses**: Validate JSON responses and handle parsing errors
4. **Proxy/Firewall Issues**: Use Unity's standard networking with proxy support

### Error Recovery Strategies

- **Silent Failures**: Network errors don't interrupt editor workflow
- **Rate Limit Protection**:
  - Strict enforcement of 12-hour minimum between checks
  - Exponential backoff on consecutive failures (1h → 4h → 12h → 24h)
  - Disable automatic checking after 5 consecutive failures
- **Retry Logic**: Automatic retry with increasing intervals for transient failures
- **Fallback Behavior**: Graceful degradation when API is unavailable
- **User Notification**: Clear messaging when rate limits are hit or checks are disabled

## Testing Strategy

### Unit Tests

1. **Version Comparison Logic**: Test version parsing and comparison
2. **Settings Persistence**: Verify EditorPrefs storage and retrieval
3. **Update Status Calculation**: Test update availability logic
4. **Error Handling**: Mock network failures and validate responses

### Integration Tests

1. **GitHub API Integration**: Test against live GitHub API (rate-limited)
2. **UI Integration**: Verify UpdateCheckerWindow displays update information correctly
3. **Startup Behavior**: Test automatic checking on editor startup
4. **Settings Persistence**: Verify settings survive editor restarts
5. **Window Functionality**: Test all UpdateCheckerWindow features and interactions

### Manual Testing Scenarios

1. **Update Available**: Mock newer version and verify UpdateCheckerWindow UI behavior
2. **No Update Available**: Verify clean UI when up-to-date in dedicated window
3. **Network Offline**: Test behavior without internet connection
4. **Settings Configuration**: Test all preference combinations within UpdateCheckerWindow
5. **Dismiss Functionality**: Verify dismissed updates don't reappear
6. **Window Access**: Test menu item access and window opening/closing behavior

## Implementation Details

### Automatic Checking Triggers

1. **Editor Startup**: Check for updates when Unity Editor starts (only if >12h since last check)
2. **Periodic Checks**: Background checking with strict 12-hour minimum intervals
3. **Manual Trigger**: Button in Settings window (respects rate limiting - shows "Check again in X hours" if too soon)
4. **Settings Change**: Re-check when automatic checking is re-enabled (only if interval allows)

### Rate Limiting Protection

- **Minimum Interval**: Enforce 12-hour minimum between any update checks
- **Check Validation**: Always validate time since last check before making requests
- **User Feedback**: Show "Last checked X hours ago" and "Next check available in X hours"
- **Manual Override**: Manual checks are subject to the same rate limiting rules
- **Failure Backoff**: Exponential backoff on consecutive API failures to prevent spam

### UI Integration Strategy

The update functionality will be contained in a dedicated UpdateCheckerWindow:

1. **Dedicated Window**: Accessible via "AirConsole/Update Checker" menu item
2. **Complete Interface**: All update-related functionality in one window
3. **Visual Design**: Consistent with existing AirConsole branding and styling
4. **Focused Experience**: Dedicated space for update management without cluttering other windows
5. **Menu Integration**: Easy access through Unity's menu system

### Performance Considerations

1. **Background Processing**: All network requests are asynchronous
2. **Caching**: Cache update status to avoid repeated API calls
3. **Rate Limiting**: Strict 12-hour minimum intervals prevent API abuse
4. **Minimal UI Impact**: Update checks don't block editor UI
5. **Resource Cleanup**: Proper disposal of web requests and callbacks
6. **Smart Scheduling**: Only check when necessary, respect GitHub's rate limits

### Security Considerations

1. **HTTPS Only**: All GitHub API requests use HTTPS
2. **User-Agent**: Proper User-Agent header for API requests
3. **Input Validation**: Validate all API responses before processing
4. **No Sensitive Data**: No authentication tokens or sensitive information stored

## Migration Strategy

### Existing Code Integration

The design minimally modifies existing code:

1. **GithubUpdate.cs**: Add public properties and initialization hooks
2. **Settings.cs**: Add update-related constants and preferences
3. **New Files**: Create UpdateChecker class, UpdateSettings ScriptableObject, and UpdateCheckerWindow

### Backward Compatibility

- All existing functionality remains unchanged
- New features are opt-in with sensible defaults
- No breaking changes to existing APIs
- Settings migration handles first-time setup

### Rollout Plan

1. **Phase 1**: Core update checking functionality
2. **Phase 2**: UI integration and settings
3. **Phase 3**: Automatic scheduling and startup triggers
4. **Phase 4**: Polish and error handling improvements