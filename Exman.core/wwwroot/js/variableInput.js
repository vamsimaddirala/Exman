// Variable input helper functions
window.getCursorPosition = function (element) {
    if (element.tagName.toLowerCase() === 'textarea' || element.tagName.toLowerCase() === 'input') {
        return element.selectionStart;
    }
    return 0;
};

window.setCursorPosition = function (element, position) {
    if (element.tagName.toLowerCase() === 'textarea' || element.tagName.toLowerCase() === 'input') {
        element.focus();
        element.setSelectionRange(position, position);
    }
};

window.getCaretCoordinates = function (element) {
    // Create a range and get the bounding client rect
    if (element.tagName.toLowerCase() === 'input') {
        // For single-line input
        let rect = element.getBoundingClientRect();
        
        // Calculate the position based on text width
        let textWidth = getTextWidthUpToCaret(element);
        
        return {
            top: rect.top,
            left: rect.left + textWidth,
            height: rect.height
        };
    } else {
        // For textarea
        let textBeforeCaret = element.value.substring(0, element.selectionStart);
        let span = document.createElement('span');
        span.style.font = window.getComputedStyle(element).font;
        span.style.position = 'absolute';
        span.style.visibility = 'hidden';
        span.style.whiteSpace = 'pre-wrap';
        span.style.width = window.getComputedStyle(element).width;
        span.textContent = textBeforeCaret;
        
        // Add a dummy character to represent cursor
        span.textContent += '|';
        
        document.body.appendChild(span);
        
        const lastCharRect = span.getBoundingClientRect();
        const elementRect = element.getBoundingClientRect();
        
        document.body.removeChild(span);
        
        return {
            top: elementRect.top + lastCharRect.height - span.clientHeight + element.scrollTop,
            left: elementRect.left + lastCharRect.width - 2,  // Adjust for the dummy character
            height: lastCharRect.height
        };
    }
};

function getTextWidthUpToCaret(input) {
    const selectionPoint = input.selectionStart;
    const textBeforeCaret = input.value.substring(0, selectionPoint);
    
    const span = document.createElement('span');
    span.style.font = window.getComputedStyle(input).font;
    span.style.position = 'absolute';
    span.style.visibility = 'hidden';
    span.style.whiteSpace = 'pre';
    span.textContent = textBeforeCaret;
    
    document.body.appendChild(span);
    const width = span.getBoundingClientRect().width;
    document.body.removeChild(span);
    
    return width;
}

// Store references to component instances
window.variableComponents = {};

// Initialize variable highlights for a specific component
window.initVariableHighlights = function (dotNetReference, containerId) {
    console.log("Initializing variable highlights for: " + containerId);
    
    // Store the component reference
    window.variableComponents[containerId] = {
        reference: dotNetReference,
        instance: document.getElementById(containerId)
    };
    
    // Set up MutationObserver to watch for changes to the highlighted content
    if (window.variableComponents[containerId].instance) {
        const container = window.variableComponents[containerId].instance;
        const observer = new MutationObserver(function(mutations) {
            attachHoverHandlersToVariables(containerId);
        });
        
        observer.observe(container, { 
            childList: true, 
            subtree: true,
            characterData: true
        });
        
        // Initial setup of hover handlers
        attachHoverHandlersToVariables(containerId);
    } else {
        console.error(`Cannot find container element with id ${containerId}`);
    }
};

function attachHoverHandlersToVariables(containerId) {
    const component = window.variableComponents[containerId];
    if (!component || !component.instance) return;
    
    const container = component.instance;
    const dotNetRef = component.reference;
    
    // Find all variable highlight spans in this container
    const highlights = container.querySelectorAll('.variable-highlight');
    console.log(`Found ${highlights.length} variable highlights in container ${containerId}`);
    
    // Add event handlers to each highlight
    highlights.forEach(highlight => {
        // Remove any existing handlers to prevent duplicates
        highlight.removeEventListener('mouseenter', highlight._mouseenterHandler);
        highlight.removeEventListener('mouseleave', highlight._mouseleaveHandler);
        
        // Create and store handler functions
        highlight._mouseenterHandler = function(e) {
            console.log('Variable mouseenter event fired');
            const variable = highlight.getAttribute('data-variable');
            const value = highlight.getAttribute('data-value');
            dotNetRef.invokeMethodAsync('HandleVariableHover', variable, value, e.clientX, e.clientY);
        };
        
        highlight._mouseleaveHandler = function() {
            console.log('Variable mouseleave event fired');
            dotNetRef.invokeMethodAsync('HandleVariableLeave');
        };
        
        // Add the event handlers
        highlight.addEventListener('mouseenter', highlight._mouseenterHandler);
        highlight.addEventListener('mouseleave', highlight._mouseleaveHandler);
        
        // Make sure the highlight is styled properly
        highlight.style.cursor = 'pointer';
        highlight.style.position = 'relative';
    });
}

// Cleanup when component is disposed
window.cleanupVariableHighlights = function(containerId) {
    // Remove the component reference
    if (window.variableComponents[containerId]) {
        delete window.variableComponents[containerId];
    }
};