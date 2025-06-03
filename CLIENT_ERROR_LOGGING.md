# Client-Side Error Logging Implementation

This document describes the client-side error logging system implemented for the Amendment application.

## Overview

The client-side error logging system captures JavaScript errors, promise rejections, and Blazor component errors, then sends them to the server for centralized logging. This helps with debugging and monitoring the application in production.

## Components

### Server-Side Components

1. **ClientErrorController** (`src/Amendment.Server/Controllers/ClientErrorController.cs`)
   - API endpoint: `POST /api/ClientError/log`
   - Handles client error reports with rate limiting
   - Supports both authenticated and anonymous users
   - Sanitizes and validates incoming error data

2. **LogClientErrorCommand** (`src/Amendment.Server.Handlers/Commands/ClientErrorCommands/LogClientErrorCommand.cs`)
   - MediatR command for processing error logging requests

3. **LogClientErrorCommandHandler** (`src/Amendment.Server.Handlers/Handlers/ClientErrorHandlers/LogClientErrorCommandHandler.cs`)
   - Processes error logging commands
   - Uses ILogger to write structured logs
   - Marks errors as originating from client-side

### Client-Side Components

1. **ClientErrorService** (`src/Amendment.Client/Services/ClientErrorService.cs`)
   - Main service for logging errors from Blazor components
   - Handles rate limiting on the client side
   - Automatically populates URL and user agent information

2. **GlobalErrorLogger** (`src/Amendment.Client/Components/GlobalErrorLogger.razor`)
   - Captures global JavaScript errors and promise rejections
   - Integrates with JavaScript error logging module

3. **AmendmentErrorBoundary** (`src/Amendment.Client/Components/AmendmentErrorBoundary.razor`)
   - Wraps components to catch and log Blazor errors
   - Provides user-friendly error display
   - Automatically logs component errors

4. **errorLogger.js** (`src/Amendment.Client/wwwroot/js/errorLogger.js`)
   - JavaScript module for capturing unhandled errors
   - Handles error queuing and rate limiting
   - Provides manual error logging functions

### Shared Models

1. **ClientErrorRequest** (`src/Amendment.Shared/Requests/ClientErrorRequest.cs`)
   - Request model containing error information
   - Includes error message, stack trace, URL, user agent, etc.

2. **ClientErrorResponse** (`src/Amendment.Shared/Responses/ClientErrorResponse.cs`)
   - Response model indicating success/failure of logging

## Features

### Error Capture
- **JavaScript Errors**: Unhandled JavaScript exceptions
- **Promise Rejections**: Unhandled promise rejections
- **Blazor Component Errors**: Errors in Blazor components
- **Manual Logging**: Programmatic error logging

### Data Collected
- Error message and stack trace
- Current user's username/identity (if authenticated)
- URL/page where the error occurred
- Browser information and user agent
- Timestamp of the error
- Component name (for Blazor errors)
- User action context
- Additional context information

### Security & Performance
- **Rate Limiting**: Both client and server-side rate limiting
- **Data Sanitization**: Input validation and size limits
- **Anonymous Support**: Works for both authenticated and anonymous users
- **Error Prevention**: Prevents infinite error loops
- **Graceful Degradation**: Continues working even if logging fails

## Usage

### Automatic Error Logging

The system automatically captures:
- Unhandled JavaScript errors
- Promise rejections
- Blazor component errors (when wrapped in AmendmentErrorBoundary)

### Manual Error Logging

#### From Blazor Components
```csharp
@inject IClientErrorService ClientErrorService

// Log a simple error
await ClientErrorService.LogErrorAsync("Something went wrong");

// Log with additional context
await ClientErrorService.LogErrorAsync(
    errorMessage: "User action failed",
    stackTrace: exception.StackTrace,
    componentName: "MyComponent",
    userAction: "Button Click",
    additionalContext: "Additional details"
);
```

#### From JavaScript
```javascript
// Log an error manually
amendmentErrorLogger.logError("Manual error message", "Additional context");

// Log user action
amendmentErrorLogger.logUserAction("Button clicked", "Additional context");
```

### Using Error Boundaries

Wrap components in AmendmentErrorBoundary to catch errors:

```razor
<AmendmentErrorBoundary ComponentName="MyComponent">
    <MyComponent />
</AmendmentErrorBoundary>
```

## Configuration

### Rate Limiting
- **Client-side**: Maximum 5 errors per minute
- **Server-side**: Maximum 10 errors per minute per IP address

### Data Limits
- Error message: 5,000 characters
- Stack trace: 10,000 characters
- URL: 2,000 characters
- User agent: 1,000 characters
- Additional context: 5,000 characters

## Testing

A test page is available at `/error-test` that provides buttons to trigger various types of errors for testing the logging system.

## Server Logs

Client-side errors are logged with the following format:
```
CLIENT_ERROR: {ErrorType} - {ErrorMessage} | User: {Username} ({UserId}) | URL: {Url} | Component: {ComponentName} | UserAgent: {UserAgent} | Details: {ErrorDetails}
```

All client errors are clearly marked with "CLIENT_ERROR:" prefix and include structured data for easy filtering and analysis.

## Integration

The error logging system is automatically initialized when the application starts:
- GlobalErrorLogger component is included in App.razor
- ClientErrorService is registered in Program.cs
- Error boundaries are added to MainLayout.razor
- JavaScript error logger is automatically loaded

No additional configuration is required for basic functionality.
