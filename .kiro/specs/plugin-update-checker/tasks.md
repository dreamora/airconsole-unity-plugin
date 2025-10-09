# Implementation Plan

- [x] 1. Create UpdateSettings ScriptableObject with persistence
  - Create UpdateSettings ScriptableObject class with serialized fields and properties
  - Implement validation logic for minimum 12-hour intervals and rate limiting
  - Add helper methods for checking time constraints (CanCheckNow, TimeUntilNextCheck)
  - Create asset loading/creation logic with default values fallback
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 2. Enhance existing GithubUpdate class with rate limiting
  - Add public properties to expose update status (IsUpdateAvailable, LatestVersion, IsCheckInProgress)
  - Implement rate limiting validation before making API requests
  - Add failure tracking and exponential backoff logic for consecutive API failures
  - Integrate with UpdateSettings for persistent configuration
  - _Requirements: 1.1, 1.3, 1.4, 5.1, 5.2, 5.3_

- [x] 3. Create UpdateChecker wrapper class
  - Implement UpdateChecker static class as main API interface
  - Add automatic checking initialization and scheduling logic
  - Implement manual check functionality with rate limiting protection
  - Add dismiss functionality for specific update versions
  - Create methods for opening GitHub releases page
  - _Requirements: 1.1, 1.2, 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Create dedicated UpdateCheckerWindow
  - Create UpdateCheckerWindow class extending EditorWindow
  - Add menu item "AirConsole/Update Checker" to access the window
  - Display current version, available version, and last check time
  - Implement update available button that opens GitHub releases page
  - Add loading indicator for checks in progress
  - Show rate limiting status ("Next check available in X hours")
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 6.1, 6.2, 6.3_

- [x] 5. Add update settings section to UpdateCheckerWindow
  - Create settings UI section for update preferences within the dedicated window
  - Add toggle for automatic update checking
  - Add manual "Check Now" button with rate limiting feedback
  - Display last check time and next available check time
  - Add dismiss current update functionality
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 6.4, 6.5_

- [x] 6. Implement automatic checking on editor startup
  - Add EditorApplication.delayCall hook for startup checking
  - Respect rate limiting rules for startup checks
  - Initialize UpdateChecker system on editor startup
  - Handle first-time setup and settings asset creation
  - _Requirements: 1.1, 4.5_

- [x] 7. Add comprehensive error handling and logging
  - Implement graceful handling of network failures and timeouts
  - Add specific handling for GitHub API rate limit responses (HTTP 403)
  - Implement exponential backoff for consecutive failures
  - Add debug logging for troubleshooting network issues
  - Handle proxy/firewall scenarios with appropriate error messages
  - _Requirements: 1.3, 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 8. Create unit tests for core functionality
  - Write tests for UpdateSettings validation and persistence
  - Test rate limiting logic and time constraint validation
  - Test version comparison and update availability detection
  - Mock network responses to test error handling scenarios
  - Test settings asset creation and loading logic
  - _Requirements: 1.4, 1.5, 4.1, 4.2, 4.3_

- [x] 9. Add periodic background checking system
  - Implement EditorApplication.update callback for periodic checks
  - Add intelligent scheduling that respects rate limits
  - Implement check interval validation and enforcement
  - Add system to disable automatic checking after persistent failures
  - Create cleanup logic for editor shutdown
  - _Requirements: 1.1, 1.2, 4.2_

- [x] 10. Polish UpdateCheckerWindow UI and user experience
  - Ensure consistent styling with existing AirConsole branding in dedicated window
  - Add tooltips and help text for update settings
  - Implement smooth transitions for update notifications
  - Add confirmation dialogs for important actions
  - Test UI responsiveness and layout in different window sizes
  - Ensure proper window sizing and docking behavior
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 6.1, 6.2, 6.3_

- [x] 11. Refactor to create actual dedicated UpdateCheckerWindow
  - Create new UpdateCheckerWindow.cs file extending EditorWindow (currently missing from codebase)
  - Extract update-specific UI components from existing SettingWindow integration
  - Add menu item "Window/AirConsole/Update Checker" to access the dedicated window
  - Implement focused update interface with current version, available version, and status display
  - Add update available button, loading indicators, and rate limiting status display
  - Include update settings section within the dedicated window (automatic checking toggle, check interval, manual check button)
  - Ensure proper window sizing, docking behavior, and consistent AirConsole branding
  - Update existing SettingWindow to remove or minimize update-related UI (keep basic notification banner only)
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
