// errorLogger.js
// Client-side error logging for the Amendment application

let dotNetReference = null;
let errorCount = 0;
const maxErrorsPerSession = 50;
const errorQueue = [];
let isProcessingQueue = false;

// Initialize error logging
export function initializeErrorLogging(dotNetRef) {
    dotNetReference = dotNetRef;
    
    // Global error handler for unhandled JavaScript errors
    window.addEventListener('error', function(event) {
        handleError({
            message: event.message || 'Unknown error',
            filename: event.filename || '',
            lineno: event.lineno || 0,
            colno: event.colno || 0,
            error: event.error,
            type: 'JavaScript Error'
        });
    });

    // Global handler for unhandled promise rejections
    window.addEventListener('unhandledrejection', function(event) {
        handleError({
            message: event.reason ? event.reason.toString() : 'Unhandled promise rejection',
            filename: '',
            lineno: 0,
            colno: 0,
            error: event.reason,
            type: 'Promise Rejection'
        });
    });

    console.log('Error logging initialized');
}

// Handle and queue errors
function handleError(errorInfo) {
    // Prevent infinite loops and excessive logging
    if (errorCount >= maxErrorsPerSession) {
        return;
    }

    errorCount++;

    // Don't log errors from the error logging system itself
    if (errorInfo.message && errorInfo.message.includes('ClientError')) {
        return;
    }

    try {
        const errorData = {
            errorMessage: errorInfo.message || 'Unknown error',
            stackTrace: getStackTrace(errorInfo.error),
            url: window.location.href,
            userAgent: navigator.userAgent,
            timestamp: new Date().toISOString(),
            errorType: errorInfo.type || 'JavaScript',
            additionalContext: JSON.stringify({
                filename: errorInfo.filename,
                lineno: errorInfo.lineno,
                colno: errorInfo.colno,
                userAgent: navigator.userAgent,
                viewport: {
                    width: window.innerWidth,
                    height: window.innerHeight
                },
                timestamp: Date.now()
            })
        };

        // Add to queue for processing
        errorQueue.push(errorData);
        processErrorQueue();

    } catch (e) {
        // Silently fail to prevent infinite error loops
        console.warn('Failed to process error for logging:', e);
    }
}

// Process the error queue
async function processErrorQueue() {
    if (isProcessingQueue || errorQueue.length === 0 || !dotNetReference) {
        return;
    }

    isProcessingQueue = true;

    try {
        while (errorQueue.length > 0) {
            const errorData = errorQueue.shift();
            
            try {
                await dotNetReference.invokeMethodAsync('LogJavaScriptError', errorData);
                // Small delay to prevent overwhelming the server
                await new Promise(resolve => setTimeout(resolve, 100));
            } catch (e) {
                // If logging fails, put the error back in the queue (up to a limit)
                if (errorQueue.length < 10) {
                    errorQueue.unshift(errorData);
                }
                break;
            }
        }
    } catch (e) {
        console.warn('Error processing error queue:', e);
    } finally {
        isProcessingQueue = false;
    }
}

// Extract stack trace from error object
function getStackTrace(error) {
    if (!error) return null;
    
    if (error.stack) {
        return error.stack;
    }
    
    if (error.stacktrace) {
        return error.stacktrace;
    }
    
    // Try to generate a stack trace
    try {
        throw new Error();
    } catch (e) {
        return e.stack || 'Stack trace not available';
    }
}

// Manual error logging function
export function logError(message, additionalContext = null) {
    handleError({
        message: message,
        filename: '',
        lineno: 0,
        colno: 0,
        error: new Error(message),
        type: 'Manual Log',
        additionalContext: additionalContext
    });
}

// Log user actions for context
export function logUserAction(action, context = null) {
    if (!dotNetReference) return;
    
    try {
        dotNetReference.invokeMethodAsync('LogUserAction', action, context);
    } catch (e) {
        // Silently fail
    }
}

// Cleanup function
export function disposeErrorLogging() {
    dotNetReference = null;
    errorQueue.length = 0;
    errorCount = 0;
    isProcessingQueue = false;
    console.log('Error logging disposed');
}

// Expose functions globally for debugging
window.amendmentErrorLogger = {
    logError: logError,
    logUserAction: logUserAction,
    getErrorCount: () => errorCount,
    getQueueLength: () => errorQueue.length
};
