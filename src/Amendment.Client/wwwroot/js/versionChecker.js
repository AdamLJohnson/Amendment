// versionChecker.js
// Handles periodic version checking for the Amendment application

let dotNetReference = null;
let versionCheckInterval = null;
let isChecking = false;

// Start periodic version checking
export function startVersionChecking(dotNetRef, intervalMinutes) {
    dotNetReference = dotNetRef;
    
    // Clear any existing interval
    if (versionCheckInterval) {
        clearInterval(versionCheckInterval);
    }
    
    // Convert minutes to milliseconds
    const intervalMs = intervalMinutes * 60 * 1000;
    
    // Set up periodic checking
    versionCheckInterval = setInterval(async () => {
        if (!isChecking && dotNetReference) {
            try {
                isChecking = true;
                await dotNetReference.invokeMethodAsync('CheckForUpdatesAsync');
            } catch (error) {
                console.warn('Version check failed:', error);
            } finally {
                isChecking = false;
            }
        }
    }, intervalMs);
    
    console.log(`Version checking started with ${intervalMinutes} minute intervals`);
}

// Stop version checking
export function stopVersionChecking() {
    if (versionCheckInterval) {
        clearInterval(versionCheckInterval);
        versionCheckInterval = null;
    }
    
    dotNetReference = null;
    isChecking = false;
    
    console.log('Version checking stopped');
}

// Manual version check
export async function checkVersionNow() {
    if (!dotNetReference || isChecking) {
        return false;
    }
    
    try {
        isChecking = true;
        return await dotNetReference.invokeMethodAsync('CheckForUpdatesAsync');
    } catch (error) {
        console.error('Manual version check failed:', error);
        return false;
    } finally {
        isChecking = false;
    }
}

// Reload the application
export function reloadApplication() {
    // Clear any cached data
    if ('caches' in window) {
        caches.keys().then(function(names) {
            for (let name of names) {
                caches.delete(name);
            }
        });
    }
    
    // Force a hard reload
    window.location.reload(true);
}

// Check if the browser supports service workers (for cache clearing)
export function supportsServiceWorkers() {
    return 'serviceWorker' in navigator;
}

// Clear service worker cache if available
export async function clearServiceWorkerCache() {
    if ('serviceWorker' in navigator) {
        try {
            const registrations = await navigator.serviceWorker.getRegistrations();
            for (let registration of registrations) {
                await registration.unregister();
            }
            console.log('Service worker cache cleared');
            return true;
        } catch (error) {
            console.warn('Failed to clear service worker cache:', error);
            return false;
        }
    }
    return false;
}

// Expose functions globally for debugging
window.amendmentVersionChecker = {
    checkVersionNow: checkVersionNow,
    reloadApplication: reloadApplication,
    clearServiceWorkerCache: clearServiceWorkerCache,
    isChecking: () => isChecking
};
