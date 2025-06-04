# Version Checking System Demo

This document provides a step-by-step demonstration of the version checking and update notification system.

## Prerequisites

1. Ensure the Amendment application is running
2. Open browser developer tools to see console logs
3. Have access to both client and server

## Demo Steps

### 1. Initial Setup Verification

**Check that the version endpoint is working:**
```bash
# Test the version endpoint directly
curl http://localhost:5165/api/version

# Expected response:
{
  "version": "1.0.0.0",
  "buildDate": "2024-01-15T10:30:00Z",
  "buildNumber": "123",
  "environment": "Development"
}
```

### 2. Client-Side Version Checking

**Open the Amendment application in your browser:**

1. Navigate to `http://localhost:5165`
2. Open browser developer tools (F12)
3. Check the console for version checking logs:
   ```
   Version checking started with 5 minute intervals
   Checking for application updates...
   Initial version stored: 1.0.0.0
   ```

### 3. Manual Version Check

**Test manual version checking via browser console:**

```javascript
// Check if version checker is available
window.amendmentVersionChecker

// Manually trigger a version check
await window.amendmentVersionChecker.checkVersionNow()

// Check current status
window.amendmentVersionChecker.isChecking()
```

### 4. Simulate Version Update

**To test the update notification:**

1. **Option A: Modify server version (for testing)**
   - Temporarily modify the version in `VersionController.cs`
   - Rebuild and restart the server
   - Wait for the next automatic check (5 minutes) or trigger manually

2. **Option B: Clear local storage (simulates first-time user)**
   ```javascript
   // Clear stored version to simulate new version detection
   localStorage.removeItem('currentAppVersion')
   
   // Trigger version check
   await window.amendmentVersionChecker.checkVersionNow()
   ```

### 5. Update Notification Flow

**When an update is detected:**

1. **Snackbar Notification Appears:**
   - Shows at the bottom of the screen
   - Displays "Update Available - New version X.X.X is ready"
   - Provides "Details" and "Update" buttons

2. **User Interaction Options:**
   - **Details**: Opens modal with version information
   - **Update**: Initiates the update process
   - **Later**: Dismisses the notification

3. **Update Process:**
   - Clears browser cache
   - Reloads the application
   - User gets the latest version

### 6. Testing Different Scenarios

**Test various user scenarios:**

#### Scenario A: User Accepts Update Immediately
1. Trigger version check
2. Click "Update" in snackbar
3. Observe cache clearing and reload

#### Scenario B: User Views Details First
1. Trigger version check
2. Click "Details" in snackbar
3. Review version information in modal
4. Click "Update Now" in modal

#### Scenario C: User Dismisses Update
1. Trigger version check
2. Click "Later" in snackbar
3. Verify notification doesn't reappear until next version

### 7. Monitoring and Debugging

**Check system behavior:**

1. **Browser Console Logs:**
   ```
   Version checking started with 5 minute intervals
   Checking for application updates...
   New version available: 1.0.1.0 (current: 1.0.0.0)
   User initiated application update from version 1.0.0.0 to 1.0.1.0
   ```

2. **Network Tab:**
   - Verify periodic GET requests to `/api/version`
   - Check request frequency (every 5 minutes)

3. **Local Storage:**
   ```javascript
   // Check stored version
   localStorage.getItem('currentAppVersion')
   ```

4. **Server Logs:**
   ```
   info: Amendment.Program[0]
         GET: /api/version 200 2ms
   ```

### 8. Error Handling Testing

**Test error scenarios:**

1. **Network Failure:**
   - Disconnect network during version check
   - Verify graceful failure and retry

2. **Server Unavailable:**
   - Stop server temporarily
   - Check error handling in console

3. **Invalid Response:**
   - Modify version endpoint to return invalid data
   - Verify error logging

### 9. Performance Verification

**Check system performance:**

1. **Minimal Impact:**
   - Version checks should not affect user interactions
   - Network requests are small and infrequent

2. **Cache Efficiency:**
   - Verify cache clearing works properly
   - Check that old cached content is removed

### 10. Production Considerations

**For production deployment:**

1. **Version Management:**
   - Ensure build process updates version numbers
   - Consider using build timestamps for more accurate detection

2. **Monitoring:**
   - Monitor version check frequency
   - Track update adoption rates

3. **User Experience:**
   - Consider update timing (avoid during critical workflows)
   - Provide clear communication about updates

## Troubleshooting

### Common Issues

1. **Version checks not working:**
   - Check if `/api/version` endpoint is accessible
   - Verify JavaScript module loading

2. **Notifications not appearing:**
   - Check browser console for errors
   - Verify component is properly registered

3. **Update process fails:**
   - Check browser cache clearing capabilities
   - Verify reload functionality

### Debug Commands

```javascript
// Check version checker status
window.amendmentVersionChecker.isChecking()

// Get error count
window.amendmentErrorLogger.getErrorCount()

// Manual cache clearing
await window.amendmentVersionChecker.clearServiceWorkerCache()

// Force reload
window.amendmentVersionChecker.reloadApplication()
```

## Success Criteria

The version checking system is working correctly when:

1. ✅ Version endpoint returns valid data
2. ✅ Client starts version checking automatically
3. ✅ Periodic checks occur every 5 minutes
4. ✅ Update notifications appear when new version detected
5. ✅ Update process clears cache and reloads successfully
6. ✅ Error handling works gracefully
7. ✅ System has minimal performance impact
8. ✅ User experience is smooth and non-intrusive
