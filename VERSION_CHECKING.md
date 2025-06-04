# Version Checking and Update Notification System

This document describes the client-side version checking and update notification system implemented for the Amendment application.

## Overview

The system automatically checks for new versions of the application and notifies users when updates are available, providing a seamless way to keep the application up-to-date without manual intervention.

## Architecture

### Server-Side Components

#### 1. VersionController (`src/Amendment.Server/Controllers/VersionController.cs`)
- **Endpoint**: `GET /api/version`
- **Access**: Anonymous (no authentication required)
- **Purpose**: Returns current server version information
- **Response**: `VersionResponse` object containing:
  - `Version`: Assembly version (e.g., "1.0.0.0")
  - `BuildDate`: When the application was built
  - `BuildNumber`: Build number from environment or assembly
  - `Environment`: Current environment name (Development, Production, etc.)

#### 2. VersionResponse (`src/Amendment.Shared/Responses/VersionResponse.cs`)
- Shared data model for version information
- Used by both client and server

### Client-Side Components

#### 1. VersionCheckService (`src/Amendment.Client/Services/VersionCheckService.cs`)
- **Interface**: `IVersionCheckService`
- **Purpose**: Core service that manages version checking
- **Features**:
  - Periodic version checking (every 5 minutes by default)
  - Local storage of current version
  - Version comparison logic
  - Event-driven notifications when updates are available

#### 2. UpdateNotification Component (`src/Amendment.Client/Components/UpdateNotification.razor`)
- **Purpose**: UI component that displays update notifications
- **Display Options**:
  - Snackbar notification (default)
  - Modal dialog (optional)
- **Features**:
  - User-friendly update prompt
  - Version information display
  - "Update Now" and "Later" options
  - Automatic cache clearing and application reload

#### 3. VersionCheckManager (`src/Amendment.Client/Components/VersionCheckManager.razor`)
- **Purpose**: Lifecycle management component
- **Responsibility**: Starts and stops version checking service
- **Integration**: Added to MainLayout for application-wide coverage

#### 4. JavaScript Module (`src/Amendment.Client/wwwroot/js/versionChecker.js`)
- **Purpose**: Handles periodic checking and cache management
- **Features**:
  - `setInterval` for periodic checks
  - Service worker cache clearing
  - Application reload functionality
  - Debug utilities

## Configuration

### Check Interval
The version check interval is configured in `VersionCheckService.cs`:
```csharp
private const int CheckIntervalMinutes = 5;
```

### Anonymous Access
The version endpoint is configured as anonymous in `HttpInterceptorService.cs`:
```csharp
private readonly List<string> _anonymousUrls = new ()
{
    // ... other URLs
    "/api/version"
};
```

## User Experience

### Notification Flow
1. **Automatic Detection**: System checks for updates every 5 minutes
2. **User Notification**: When an update is detected:
   - Snackbar appears with update information
   - User can choose "Details" to see more info or "Update" to proceed
3. **Update Process**: When user clicks "Update Now":
   - Application cache is cleared
   - Page is reloaded with the new version
   - User's work is preserved (no data loss)

### Notification Types
- **Snackbar**: Non-intrusive notification that appears at the bottom
- **Modal**: More prominent dialog (can be enabled via component parameters)

## Version Comparison Logic

The system uses the following logic to determine if an update is available:

1. **Primary**: Compare build dates (most reliable for deployments)
2. **Secondary**: Compare version strings if build dates are identical
3. **Storage**: Current version is stored in browser local storage

## Integration Points

### Service Registration
In `Program.cs`:
```csharp
builder.Services.AddScoped<IVersionCheckService, VersionCheckService>();
```

### Layout Integration
In `MainLayout.razor`:
```razor
<UpdateNotification ShowAsSnackbar="true" ShowAsModal="false"></UpdateNotification>
<VersionCheckManager></VersionCheckManager>
```

## Error Handling

### Network Failures
- Version checks fail gracefully
- Errors are logged but don't interrupt user workflow
- Automatic retry on next scheduled check

### JavaScript Errors
- Service continues working even if individual checks fail
- Fallback mechanisms for cache clearing
- Debug utilities available in browser console

## Testing

### Unit Tests
- `VersionControllerTests.cs`: Tests for the version endpoint
- Validates response format and anonymous access

### Manual Testing
1. **Version Detection**: Deploy new version and verify notification appears
2. **Update Process**: Click "Update Now" and verify reload works
3. **Cache Clearing**: Verify old cached content is removed

## Debugging

### Browser Console
Access debug utilities via:
```javascript
window.amendmentVersionChecker.checkVersionNow()
window.amendmentVersionChecker.reloadApplication()
window.amendmentVersionChecker.clearServiceWorkerCache()
```

### Logging
- Client-side logs available in browser console
- Server-side logs in application logs
- Version check events are logged with appropriate levels

## Security Considerations

- Version endpoint is anonymous but only exposes non-sensitive build information
- No user data is transmitted during version checks
- Cache clearing is limited to application cache only

## Performance Impact

- **Minimal**: Checks occur every 5 minutes with small HTTP requests
- **Efficient**: Uses browser's built-in caching mechanisms
- **Non-blocking**: Version checks don't impact user interactions

## Future Enhancements

Potential improvements for the system:
- Configurable check intervals
- Different notification styles based on update criticality
- Rollback capability
- Update scheduling (e.g., update during off-hours)
- Progressive web app update integration
