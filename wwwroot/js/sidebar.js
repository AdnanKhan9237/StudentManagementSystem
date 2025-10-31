// Sidebar Toggle Functionality
document.addEventListener('DOMContentLoaded', function () {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const mainContent = document.querySelector('.main-content');
    const body = document.body;
    let backdrop = null;

    // Toggle sidebar on button click
    function ensureBackdrop() {
        if (!backdrop) {
            backdrop = document.createElement('div');
            backdrop.className = 'sidebar-backdrop';
            backdrop.addEventListener('click', closeMobileSidebar);
        }
        return backdrop;
    }

    function openMobileSidebar() {
        sidebar.classList.add('show');
        body.classList.add('no-scroll');
        document.body.appendChild(ensureBackdrop());
    }

    function closeMobileSidebar() {
        sidebar.classList.remove('show');
        body.classList.remove('no-scroll');
        if (backdrop && backdrop.parentNode) backdrop.parentNode.removeChild(backdrop);
    }

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            if (window.innerWidth > 991) {
                sidebar.classList.toggle('collapsed');
                mainContent.classList.toggle('expanded');
            } else {
                if (sidebar.classList.contains('show')) {
                    closeMobileSidebar();
                } else {
                    openMobileSidebar();
                }
            }
        });
    }

    // Handle dropdown menus
    const dropdownToggles = document.querySelectorAll('.sidebar-dropdown-toggle');
    dropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            const parent = this.closest('.sidebar-dropdown');
            
            // Close other dropdowns
            document.querySelectorAll('.sidebar-dropdown').forEach(dropdown => {
                if (dropdown !== parent) {
                    dropdown.classList.remove('active');
                }
            });
            
            // Toggle current dropdown
            parent.classList.toggle('active');
        });
    });

    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function (e) {
        if (window.innerWidth <= 991) {
            if (!sidebar.contains(e.target) && !sidebarToggle.contains(e.target)) {
                closeMobileSidebar();
            }
        }
    });

    // Close on Esc
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && window.innerWidth <= 991 && sidebar.classList.contains('show')) {
            closeMobileSidebar();
        }
    });

    // Handle window resize
    let resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            if (window.innerWidth > 991) {
                closeMobileSidebar();
                sidebar.classList.remove('collapsed');
                mainContent.classList.remove('expanded');
            }
        }, 250);
    });

    // Highlight active menu item based on current URL
    const currentPath = window.location.pathname;
    const menuLinks = document.querySelectorAll('.sidebar-menu a');
    
    menuLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && currentPath.includes(href) && href !== '#') {
            link.classList.add('active');
            
            // Open parent dropdown if item is in submenu
            const submenu = link.closest('.sidebar-submenu');
            if (submenu) {
                const dropdown = submenu.closest('.sidebar-dropdown');
                if (dropdown) {
                    dropdown.classList.add('active');
                }
            }
        }

        // Close sidebar after navigating on mobile
        link.addEventListener('click', function () {
            if (window.innerWidth <= 991) {
                closeMobileSidebar();
            }
        });
    });
});
