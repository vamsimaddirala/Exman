/**
 * Sidebar resizing functionality for Exman
 */
window.sidebarResizer = {
    initialize: function() {
        console.log('Initializing sidebar resizer...');
        // Find all sidebar-resizer elements
        const resizers = document.querySelectorAll('.sidebar-resizer');
        console.log('Found resizers:', resizers.length);
        
        resizers.forEach(function(resizer) {
            // Add mouse down event listener to each resizer
            resizer.addEventListener('mousedown', function(e) {
                e.preventDefault();
                // Add active class to the resizer
                resizer.classList.add('active');
                
                // Get the sidebar element (previous sibling)
                const sidebar = resizer.previousElementSibling;
                
                // Initial width of sidebar
                const initialWidth = sidebar.offsetWidth;
                // Initial mouse position
                const initialMouseX = e.clientX;
                
                // Store the current width in a data attribute
                sidebar.dataset.currentWidth = initialWidth;
                
                // Create mousemove event handler
                function handleMouseMove(e) {
                    // Calculate the new width based on mouse movement
                    const currentMouseX = e.clientX;
                    const mouseDelta = currentMouseX - initialMouseX;
                    const newWidth = Math.max(150, Math.min(500, Number(sidebar.dataset.currentWidth) + mouseDelta));
                    
                    // Apply the new width
                    sidebar.style.width = newWidth + 'px';
                    
                    // Store the new width in local storage for persistence
                    try {
                        localStorage.setItem('sidebar-width', newWidth);
                    } catch (e) {
                        console.error('Failed to save sidebar width to localStorage:', e);
                    }
                }
                
                // Create mouseup event handler to clean up
                function handleMouseUp() {
                    console.log('Mouse up - resize complete');
                    resizer.classList.remove('active');
                    document.removeEventListener('mousemove', handleMouseMove);
                    document.removeEventListener('mouseup', handleMouseUp);
                }
                
                // Add temporary event listeners for mousemove and mouseup
                document.addEventListener('mousemove', handleMouseMove);
                document.addEventListener('mouseup', handleMouseUp);
            });
        });
        
        // Restore sidebar width from localStorage
        // try {
        //     const savedWidth = 80;//localStorage.getItem('sidebar-width');
        //     if (savedWidth) {
        //         const sidebars = document.querySelectorAll('.sidebar');
        //         sidebars.forEach(function(sidebar) {
        //             sidebar.style.width = savedWidth + 'px';
        //         });
        //     }
        // } catch (e) {
        //     console.error('Failed to restore sidebar width from localStorage:', e);
        // }
    },
    
    // New method to manually initialize after Blazor rendering
    initializeAfterBlazor: function() {
        console.log('Manual initialization after Blazor rendering');
        this.initialize();
    }
};

// Listen for both DOMContentLoaded and a custom event for Blazor initialization
document.addEventListener('DOMContentLoaded', function() {
    window.sidebarResizer.initialize();
});

// Define a method that can be called from .NET
window.initializeSidebarResizer = function() {
    window.sidebarResizer.initialize();
};