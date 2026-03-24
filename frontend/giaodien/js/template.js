/**
 * Template JavaScript - Render sidebar và các component chung
 * Version: 2.0
 */

// Menu items configuration
const MENU_ITEMS = [
    { href: 'index.html', icon: 'fa-home', text: 'Trang chủ' },
    { href: 'phong.html', icon: 'fa-door-open', text: 'Quản lý phòng' },
    { href: 'loaiPhong.html', icon: 'fa-list', text: 'Loại phòng' },
    { href: 'nguoiThue.html', icon: 'fa-users', text: 'Người thuê' },
    { href: 'hopDong.html', icon: 'fa-file-contract', text: 'Hợp đồng' },
    { href: 'hoaDon.html', icon: 'fa-file-invoice-dollar', text: 'Hóa đơn' },
    { href: 'thanhToan.html', icon: 'fa-money-bill-wave', text: 'Thanh toán' },
    { href: 'chisodien.html', icon: 'fa-bolt', text: 'Chỉ số điện' },
    { href: 'chisonuoc.html', icon: 'fa-tint', text: 'Chỉ số nước' },
    { href: 'dichVu.html', icon: 'fa-concierge-bell', text: 'Dịch vụ' },
    { href: 'quanLyTaiKhoan.html', icon: 'fa-user-cog', text: 'Quản lý tài khoản' }
];

/**
 * Render sidebar navigation
 * @param {string} activePage - Tên file HTML đang active (ví dụ: 'chisodien.html')
 */
function renderSidebar(activePage = '') {
    const currentPage = activePage || getCurrentPage();
    
    const sidebarHTML = `
        <nav class="col-md-3 col-lg-2 d-md-block sidebar collapse">
            <div class="position-sticky pt-3">
                <div class="sidebar-brand">
                    <h4><i class="fas fa-building"></i> QL Nhà Trọ</h4>
                </div>
                
                <div class="user-info">
                    <span class="fw-bold" id="userFullname">User</span>
                    <button class="logout-btn" onclick="logout()">
                        <i class="fas fa-sign-out-alt"></i> Thoát
                    </button>
                </div>

                <ul class="nav flex-column">
                    ${MENU_ITEMS.map(item => `
                        <li class="nav-item">
                            <a class="nav-link ${currentPage === item.href ? 'active' : ''}" href="${item.href}">
                                <i class="fas ${item.icon}"></i> ${item.text}
                            </a>
                        </li>
                    `).join('')}
                </ul>
            </div>
        </nav>
    `;
    
    return sidebarHTML;
}

/**
 * Render page header
 * @param {string} title - Tiêu đề trang
 * @param {string} actionButton - HTML cho nút action (tùy chọn)
 */
function renderPageHeader(title, actionButton = '') {
    return `
        <div class="page-header">
            <h1 class="h2 text-primary">${title}</h1>
            ${actionButton ? `<div>${actionButton}</div>` : ''}
        </div>
    `;
}

/**
 * Get current page name from URL
 */
function getCurrentPage() {
    const path = window.location.pathname;
    const page = path.split('/').pop();
    return page || 'index.html';
}

/**
 * Check authentication and redirect if not logged in
 */
function checkAuth() {
    const token = localStorage.getItem('token');
    let user = null;
    
    try {
        user = JSON.parse(localStorage.getItem('user'));
    } catch (e) {
        console.error('Error parsing user data:', e);
    }

    if (!token || !user) {
        window.location.href = 'login.html';
        return false;
    }
    
    // Update user fullname in sidebar
    const userFullnameEl = document.getElementById('userFullname');
    if (userFullnameEl) {
        userFullnameEl.textContent = user.fullname || user.Fullname || 'Admin';
    }
    
    return true;
}

/**
 * Logout function
 */
window.logout = function() {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
        localStorage.clear();
        window.location.href = 'login.html';
    }
};

/**
 * Initialize template (sidebar, auth check)
 * @param {string} activePage - Tên file HTML đang active
 */
function initTemplate(activePage = '') {
    // Check auth first
    if (!checkAuth()) {
        return;
    }
    
    // Auto-detect active page if not provided
    if (!activePage) {
        activePage = getCurrentPage();
    }
    
    // Render sidebar if container exists
    const sidebarContainer = document.getElementById('sidebar-container');
    if (sidebarContainer) {
        sidebarContainer.innerHTML = renderSidebar(activePage);
        
        // Update user fullname after sidebar is rendered
        setTimeout(() => {
            const userFullnameEl = document.getElementById('userFullname');
            if (userFullnameEl) {
                const user = JSON.parse(localStorage.getItem('user') || '{}');
                userFullnameEl.textContent = user.fullname || user.Fullname || 'Admin';
            }
        }, 100);
    }
    
    // Set active class on current page link (after sidebar is rendered)
    setTimeout(() => {
        const currentPage = activePage || getCurrentPage();
        document.querySelectorAll('.sidebar .nav-link').forEach(link => {
            if (link.getAttribute('href') === currentPage) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    }, 100);
}

/**
 * Utility: Show alert message
 * @param {string} message - Message to display
 * @param {string} type - Alert type: 'success', 'danger', 'warning', 'info'
 * @param {number} duration - Duration in milliseconds (default: 3000)
 */
function showAlert(message, type = 'success', duration = 3000) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);';
    alertDiv.innerHTML = `
        <strong>${type === 'success' ? '✓' : type === 'danger' ? '✗' : type === 'warning' ? '⚠' : 'ℹ'}</strong> ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    document.body.appendChild(alertDiv);
    
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.classList.remove('show');
            setTimeout(() => alertDiv.remove(), 150);
        }
    }, duration);
}

/**
 * Utility: Format currency (VND)
 * @param {number} amount - Amount to format
 * @returns {string} Formatted currency string
 */
function formatCurrency(amount) {
    if (amount === null || amount === undefined || isNaN(amount)) {
        return '0 ₫';
    }
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

/**
 * Utility: Format date (Vietnamese)
 * @param {string|Date} date - Date to format
 * @returns {string} Formatted date string
 */
function formatDate(date) {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    });
}

/**
 * Utility: Format date for input[type="date"]
 * @param {string|Date} date - Date to format
 * @returns {string} Formatted date string (YYYY-MM-DD)
 */
function formatDateForInput(date) {
    if (!date) return '';
    const d = new Date(date);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

/**
 * Utility: Confirm dialog
 * @param {string} message - Message to display
 * @param {string} title - Dialog title
 * @returns {Promise<boolean>} User's choice
 */
function confirmDialog(message, title = 'Xác nhận') {
    return new Promise((resolve) => {
        const confirmed = confirm(`${title}\n\n${message}`);
        resolve(confirmed);
    });
}

/**
 * Utility: Loading overlay
 * @param {boolean} show - Show or hide loading
 */
function showLoading(show = true) {
    let overlay = document.getElementById('loading-overlay');
    
    if (show) {
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'loading-overlay';
            overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.5);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 99999;
            `;
            overlay.innerHTML = `
                <div class="spinner-border text-light" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
            `;
            document.body.appendChild(overlay);
        } else {
            overlay.style.display = 'flex';
        }
    } else {
        if (overlay) {
            overlay.style.display = 'none';
        }
    }
}

// Export functions for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        renderSidebar,
        renderPageHeader,
        getCurrentPage,
        checkAuth,
        initTemplate,
        showAlert,
        formatCurrency,
        formatDate,
        formatDateForInput,
        confirmDialog,
        showLoading,
        MENU_ITEMS
    };
}

// Make functions globally available
window.showAlert = showAlert;
window.formatCurrency = formatCurrency;
window.formatDate = formatDate;
window.formatDateForInput = formatDateForInput;
window.confirmDialog = confirmDialog;
window.showLoading = showLoading;

