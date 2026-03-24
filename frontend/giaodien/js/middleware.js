// Middleware kiểm tra quyền truy cập
function requireAuth() {
    if (!Auth.checkAuth()) {
        return false;
    }
    return true;
}

// Middleware kiểm tra quyền admin
function requireAdmin() {
    if (!requireAuth()) {
        return false;
    }
    if (!Auth.isAdmin()) {
        window.location.href = 'user-dashboard.html';
        return false;
    }
    return true;
}

// Middleware kiểm tra quyền user
function requireUser() {
    if (!requireAuth()) {
        return false;
    }
    if (!Auth.isUser()) {
        window.location.href = 'index.html';
        return false;
    }
    return true;
}

// Hàm khởi tạo middleware cho trang
function initMiddleware(requiredRole) {
    switch (requiredRole) {
        case 'admin':
            return requireAdmin();
        case 'user':
            return requireUser();
        default:
            return requireAuth();
    }
}

// Export các hàm middleware
window.Middleware = {
    requireAuth,
    requireAdmin,
    requireUser,
    initMiddleware
}; 