// Các role trong hệ thống
const ROLES = {
    ADMIN: 'admin',
    USER: 'user'
};

// Lưu trữ thông tin người dùng
let currentUser = null;

// Hàm kiểm tra đăng nhập
async function login(username, password) {
    try {
        console.log('Đang gửi request đăng nhập...');
        console.log('URL:', `${Config.API_URL}${Config.ENDPOINTS.AUTH}/dang-nhap`);
        
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.AUTH}/dang-nhap`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({ 
                tenDangNhap: username, 
                matKhau: password 
            })
        });

        console.log('Response status:', response.status);
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            console.error('Lỗi response:', errorData);
            throw new Error(errorData?.thongBao || 'Đăng nhập thất bại');
        }

        const data = await response.json();
        console.log('Đăng nhập thành công:', data);
        
        if (!data.token || !data.user) {
            throw new Error('Dữ liệu đăng nhập không hợp lệ');
        }

        // Lưu token và thông tin người dùng
        localStorage.setItem('token', data.token);
        localStorage.setItem('user', JSON.stringify(data.user));
        currentUser = data.user;

        return data;
    } catch (error) {
        console.error('Lỗi đăng nhập:', error);
        throw error;
    }
}

// Hàm đăng xuất
function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    currentUser = null;
    window.location.href = '/login.html';
}

// Hàm kiểm tra quyền truy cập
function checkAuth() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/login.html';
        return false;
    }
    return true;
}

// Hàm kiểm tra role
function hasRole(role) {
    if (!currentUser) {
        const userStr = localStorage.getItem('user');
        if (userStr) {
            currentUser = JSON.parse(userStr);
        }
    }
    return currentUser && currentUser.role === role;
}

// Hàm kiểm tra quyền admin
function isAdmin() {
    return hasRole(ROLES.ADMIN);
}

// Hàm kiểm tra quyền user
function isUser() {
    return hasRole(ROLES.USER);
}

// Hàm lấy thông tin người dùng hiện tại
function getCurrentUser() {
    if (!currentUser) {
        const userStr = localStorage.getItem('user');
        if (userStr) {
            currentUser = JSON.parse(userStr);
        }
    }
    return currentUser;
}

// Export các hàm để sử dụng ở các file khác
window.Auth = {
    login,
    logout,
    checkAuth,
    hasRole,
    isAdmin,
    isUser,
    getCurrentUser,
    ROLES
}; 