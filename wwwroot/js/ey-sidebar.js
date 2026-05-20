/* ========================================
   EY SIDEBAR - JavaScript Functionality
   ======================================== */

// Toggle submenu functionality
function toggleSubmenu(menuId) {
    const submenu = document.getElementById(`submenu-${menuId}`);
    const chevron = document.getElementById(`chevron-${menuId}`);
    
    if (submenu.classList.contains('expanded')) {
        submenu.classList.remove('expanded');
        chevron.classList.remove('rotated');
    } else {
        // Close other submenus (optional - remove if you want multiple open)
        document.querySelectorAll('.ey-submenu.expanded').forEach(el => {
            el.classList.remove('expanded');
        });
        document.querySelectorAll('.ey-chevron.rotated').forEach(el => {
            el.classList.remove('rotated');
        });
        
        submenu.classList.add('expanded');
        chevron.classList.add('rotated');
    }
}

// Mobile sidebar toggle
function toggleSidebar() {
    const sidebar = document.querySelector('.ey-sidebar');
    const overlay = document.querySelector('.ey-overlay');
    
    sidebar.classList.toggle('active');
    overlay.classList.toggle('active');
}

// Close sidebar when clicking overlay
function closeSidebar() {
    const sidebar = document.querySelector('.ey-sidebar');
    const overlay = document.querySelector('.ey-overlay');
    
    sidebar.classList.remove('active');
    overlay.classList.remove('active');
}

// Toggle chatbot (placeholder function)
function toggleChatbot() {
    // This function should integrate with existing chatbot functionality
    console.log('Toggle chatbot');
    // You can call your existing chatbot toggle function here
    if (typeof toggleChatbotWidget === 'function') {
        toggleChatbotWidget();
    }
}

// Initialize sidebar on page load
document.addEventListener('DOMContentLoaded', function() {
    // Add mobile toggle button if it doesn't exist
    if (!document.querySelector('.ey-sidebar-toggle')) {
        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'ey-sidebar-toggle';
        toggleBtn.innerHTML = '<i class="fas fa-bars"></i>';
        toggleBtn.onclick = toggleSidebar;
        document.body.appendChild(toggleBtn);
    }
    
    // Add overlay if it doesn't exist
    if (!document.querySelector('.ey-overlay')) {
        const overlay = document.createElement('div');
        overlay.className = 'ey-overlay';
        overlay.onclick = closeSidebar;
        document.body.appendChild(overlay);
    }
    
    // Auto-expand submenu based on current page
    const currentController = getCurrentController();
    const currentAction = getCurrentAction();
    
    // Expand relevant submenu based on current page
    expandRelevantSubmenu(currentController, currentAction);
    
    // Handle window resize
    handleResize();
});

// Get current controller from URL
function getCurrentController() {
    const path = window.location.pathname;
    const segments = path.split('/').filter(s => s);
    return segments.length > 0 ? segments[0] : 'Home';
}

// Get current action from URL
function getCurrentAction() {
    const path = window.location.pathname;
    const segments = path.split('/').filter(s => s);
    return segments.length > 1 ? segments[1] : 'Index';
}

// Expand submenu based on current page
function expandRelevantSubmenu(controller, action) {
    const submenuMap = {
        'Collaborateurs': 'core-rh',
        'Formations': 'formations',
        'Competences': 'competences',
        'Talent': 'competences',
        'Reporting': 'analytics',
        'RHInsights': 'analytics'
    };
    
    const menuId = submenuMap[controller];
    if (menuId) {
        const submenu = document.getElementById(`submenu-${menuId}`);
        const chevron = document.getElementById(`chevron-${menuId}`);
        
        if (submenu && chevron) {
            submenu.classList.add('expanded');
            chevron.classList.add('rotated');
        }
    }
}

// Handle window resize for responsive behavior
function handleResize() {
    const sidebar = document.querySelector('.ey-sidebar');
    const overlay = document.querySelector('.ey-overlay');
    
    window.addEventListener('resize', function() {
        if (window.innerWidth > 992) {
            sidebar.classList.remove('active');
            overlay.classList.remove('active');
        }
    });
}

// Keyboard navigation
document.addEventListener('keydown', function(e) {
    // Close sidebar with Escape key
    if (e.key === 'Escape') {
        const sidebar = document.querySelector('.ey-sidebar');
        const overlay = document.querySelector('.ey-overlay');
        
        if (sidebar.classList.contains('active')) {
            closeSidebar();
        }
    }
});

// Add smooth scroll to main content when sidebar is toggled
function smoothScrollToContent() {
    const mainContent = document.querySelector('.ey-main');
    if (mainContent) {
        mainContent.scrollIntoView({ behavior: 'smooth' });
    }
}
