// screenControlKeyboard.js
// Handles keyboard events for the Screen Control page

// Initialize keyboard event handling
export function initializeKeyboardHandling(dotNetReference) {
    // Store the dotNetReference for later use
    window.screenControlDotNetReference = dotNetReference;
    
    // Add the event listener to the document
    document.addEventListener('keydown', handleKeyDown);
    
    console.log("Screen Control keyboard handling initialized");
}

// Remove keyboard event handling when component is disposed
export function disposeKeyboardHandling() {
    document.removeEventListener('keydown', handleKeyDown);
    window.screenControlDotNetReference = null;
    
    console.log("Screen Control keyboard handling disposed");
}

// Handle keydown events
function handleKeyDown(event) {
    // Only process if we have a reference to the .NET object
    if (!window.screenControlDotNetReference) return;
    
    // Check if the user is typing in an input field or textarea
    if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') {
        return;
    }
    
    // Handle specific keys
    switch (event.key) {
        case ' ': // Space bar
            event.preventDefault(); // Prevent page scrolling
            window.screenControlDotNetReference.invokeMethodAsync('HandleSpaceKey');
            break;
            
        case '1': // 1 key
            window.screenControlDotNetReference.invokeMethodAsync('HandleOneKey');
            break;
            
        case 'ArrowRight': // Right arrow key
            window.screenControlDotNetReference.invokeMethodAsync('HandleRightArrowKey');
            break;
    }
}
