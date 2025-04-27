// Handle resizing between request and response sections
function initializeRequestResponseResizer() {
    const resizer = document.querySelector('.request-response-resizer');
    const requestSection = document.querySelector('.request-section');
    const responseSection = document.querySelector('.response-section');
    const mainContent = document.querySelector('.main-content');
    
    if (!resizer || !requestSection || !responseSection || !mainContent) {
        return; // Elements not found
    }

    // Initial proportions - default to 40% for request, 60% for response
    // Check if there are saved proportions in localStorage
    let requestProportion = localStorage.getItem('requestProportion') || 0.4;
    let responseProportion = 1 - requestProportion;

    // Apply initial proportions
    applyProportions();

    let startY = 0;
    let startRequestHeight = 0;
    let mainContentHeight = 0;
    
    function startResize(e) {
        // Calculate the actual available height for distribution
        const urlBarHeight = document.querySelector('.url-bar')?.offsetHeight || 0;
        const tabsContainerHeight = document.querySelector('.request-tabs-container')?.offsetHeight || 0;
        mainContentHeight = mainContent.offsetHeight - urlBarHeight - tabsContainerHeight;
        
        startY = e.clientY;
        startRequestHeight = requestSection.offsetHeight;
        
        document.addEventListener('mousemove', resize);
        document.addEventListener('mouseup', stopResize);
        
        // Add active class for styling
        resizer.classList.add('active');
        
        // Prevent text selection during resize
        document.body.style.userSelect = 'none';
        e.preventDefault();
    }
    
    function resize(e) {
        if (mainContentHeight <= 0) return;
        
        const deltaY = e.clientY - startY;
        const newRequestHeight = startRequestHeight + deltaY;
        
        // Calculate new proportions
        const minRequestHeight = 40; // Minimum request section height
        const minResponseHeight = 40; // Minimum response section height
        
        // Ensure minimum heights are respected
        if (newRequestHeight < minRequestHeight || 
            (mainContentHeight - newRequestHeight - resizer.offsetHeight) < minResponseHeight) {
            return;
        }
        
        // Set new proportions - accounting for resizer height
        requestProportion = newRequestHeight / mainContentHeight;
        responseProportion = 1 - requestProportion - (resizer.offsetHeight / mainContentHeight);
        
        // Save to localStorage for persistence
        localStorage.setItem('requestProportion', requestProportion);
        
        applyProportions();
    }
    
    function stopResize() {
        document.removeEventListener('mousemove', resize);
        document.removeEventListener('mouseup', stopResize);
        
        // Remove active class
        resizer.classList.remove('active');
        
        // Restore text selection
        document.body.style.userSelect = '';
    }
    
    function applyProportions() {
        // Ensure the resizer has a fixed height
        const resizerHeight = 2; // Height in pixels for the resizer
        
        // Make sure resizer is visible and has height
        resizer.style.height = `${resizerHeight}px`;
        resizer.style.minHeight = `${resizerHeight}px`;
        
        // Calculate the actual percentages accounting for the resizer
        const resizerProportion = resizerHeight / mainContent.offsetHeight;
        const adjustedRequestProportion = requestProportion * (1 - resizerProportion);
        const adjustedResponseProportion = responseProportion * (1 - resizerProportion);
        
        // Use flex-basis instead of height for better flexbox behavior
        requestSection.style.flexBasis = `calc(${adjustedRequestProportion * 100}% - ${resizerHeight/2}px)`;
        requestSection.style.flexGrow = 0;
        requestSection.style.flexShrink = 0;
        
        responseSection.style.flexBasis = `calc(${adjustedResponseProportion * 100}% - ${resizerHeight/2}px)`;
        responseSection.style.flexGrow = 0;
        responseSection.style.flexShrink = 0;
    }
    
    // Handle window resize
    window.addEventListener('resize', function() {
        // Recalculate mainContentHeight on window resize
        const urlBarHeight = document.querySelector('.url-bar')?.offsetHeight || 0;
        const tabsContainerHeight = document.querySelector('.request-tabs-container')?.offsetHeight || 0;
        mainContentHeight = mainContent.offsetHeight - urlBarHeight - tabsContainerHeight;
        
        applyProportions();
    });
    
    // Set up the event listener
    resizer.addEventListener('mousedown', startResize);
}

// Initialize the sidebar resizer
function initializeSidebarResizer() {
    const resizer = document.querySelector('.sidebar-resizer');
    const sidebar = document.querySelector('.sidebar');
    
    if (!resizer || !sidebar) {
        return; // Elements not found
    }
    
    let startX = 0;
    let startWidth = 0;
    
    function startResize(e) {
        startX = e.clientX;
        startWidth = sidebar.offsetWidth;
        
        document.addEventListener('mousemove', resize);
        document.addEventListener('mouseup', stopResize);
        
        // Add active class for styling
        resizer.classList.add('active');
        
        // Prevent text selection during resize
        document.body.style.userSelect = 'none';
        e.preventDefault();
    }
    
    function resize(e) {
        const newWidth = startWidth + (e.clientX - startX);
        const minWidth = 80; // Minimum sidebar width
        const maxWidth = 500; // Maximum sidebar width
        
        if (newWidth >= minWidth && newWidth <= maxWidth) {
            sidebar.style.width = `${newWidth}px`;
        }
    }
    
    function stopResize() {
        document.removeEventListener('mousemove', resize);
        document.removeEventListener('mouseup', stopResize);
        
        // Remove active class
        resizer.classList.remove('active');
        
        // Restore text selection
        document.body.style.userSelect = '';
    }
    
    resizer.addEventListener('mousedown', startResize);
}

// Initialize both resizers when the document is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeSidebarResizer();
    initializeRequestResponseResizer();
});

// Export the functions for Blazor interop
window.initializeSidebarResizer = initializeSidebarResizer;
window.initializeRequestResponseResizer = initializeRequestResponseResizer;