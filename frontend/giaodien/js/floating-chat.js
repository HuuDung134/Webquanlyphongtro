// Sử dụng function để tránh conflict khi load nhiều lần
function getFloatingChatAPI_URL() {
    return typeof Config !== 'undefined' ? Config.API_URL : '';
}

function getFloatingChatTIN_NHAN_ENDPOINT() {
    return typeof Config !== 'undefined' && Config.ENDPOINTS ? Config.ENDPOINTS.TIN_NHAN : '/api/TinNhan';
}

// Tạo floating chat button
function createFloatingChatButton() {
    // Kiểm tra đăng nhập
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    
    if (!token) return;
    
    // Tạo container
    const container = document.createElement('div');
    container.className = 'floating-chat-container';
    container.id = 'floatingChatContainer';
    
    // Xác định vai trò (kiểm tra cả role và vaiTro để tương thích)
    const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');
    
    // Tạo button chính (Tin nhắn) - mở modal
    const mainButton = createChatButton(
        '#',
        isAdmin ? 'admin' : 'user',
        isAdmin ? 'Tin nhắn Admin' : 'Tin nhắn',
        'chatBadge',
        'fas fa-comments'
    );
    
    mainButton.addEventListener('click', (e) => {
        e.preventDefault();
        console.log('Chat button clicked, openChatModal type:', typeof openChatModal);
        // Mở modal chat
        if (typeof openChatModal !== 'undefined' && typeof window.openChatModal !== 'undefined') {
            console.log('Calling openChatModal...');
            window.openChatModal();
        } else {
            console.error('chat-modal.js chưa được load, openChatModal:', typeof openChatModal, 'window.openChatModal:', typeof window.openChatModal);
            // Fallback: chuyển đến trang chat
            window.location.href = isAdmin ? 'tinNhanAdmin.html' : 'tinNhanKhach.html';
        }
    });
    
    container.appendChild(mainButton);
    
    // Không tạo notification button ở đây nữa - đã chuyển lên góc trên bên phải
    
    document.body.appendChild(container);
    
    // Load số tin nhắn chưa đọc
    if (isAdmin) {
        loadUnreadCountAdmin();
        // Polling mỗi 10 giây
        setInterval(loadUnreadCountAdmin, 10000);
    } else {
        loadUnreadCountUser();
        // Polling mỗi 10 giây
        setInterval(loadUnreadCountUser, 10000);
    }
}

// Tạo một chat button
function createChatButton(url, type, label, badgeId, icon) {
    const button = document.createElement('button');
    button.className = `floating-chat-btn ${type}`;
    button.innerHTML = `
        <i class="${icon}"></i>
        <span class="chat-badge hidden" id="${badgeId}">0</span>
        <span class="chat-tooltip">${label}</span>
    `;
    
    button.addEventListener('click', () => {
        if (url !== '#') {
            window.location.href = url;
        }
    });
    
    return button;
}

// Load số tin nhắn chưa đọc cho Admin
async function loadUnreadCountAdmin() {
    try {
        const token = localStorage.getItem('token');
        if (!token) return;
        
        const response = await fetch(`${getFloatingChatAPI_URL()}${getFloatingChatTIN_NHAN_ENDPOINT()}/admin/danh-sach-hoi-thoai`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) return;
        
        const data = await response.json();
        const conversations = data.danhSachHoiThoai || [];
        const totalUnread = conversations.reduce((sum, conv) => sum + (conv.soLuongChuaDoc || 0), 0);
        
        updateBadge(totalUnread);
    } catch (error) {
        console.error('Error loading unread count:', error);
    }
}

// Load số tin nhắn chưa đọc cho User
async function loadUnreadCountUser() {
    try {
        const token = localStorage.getItem('token');
        const user = JSON.parse(localStorage.getItem('user') || '{}');
        
        if (!token) return;
        
        // Kiểm tra lại vai trò để đảm bảo không phải Admin
        const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');
        if (isAdmin) {
            // Nếu là Admin, không gọi endpoint này
            return;
        }
        
        const response = await fetch(`${getFloatingChatAPI_URL()}${getFloatingChatTIN_NHAN_ENDPOINT()}/khach/voi-admin?take=100`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            // Nếu lỗi 404 hoặc 500, có thể là người dùng chưa có bản ghi NguoiThue
            if (response.status === 404 || response.status === 500) {
                console.warn('Không thể tải tin nhắn: Người dùng có thể chưa có thông tin người thuê');
            }
            return;
        }
        
        const data = await response.json();
        const messages = data.danhSachTinNhan || [];
        const unreadCount = messages.filter(msg => !msg.daDocAt && !msg.laKhachGui).length;
        
        updateBadge(unreadCount);
    } catch (error) {
        // Chỉ log lỗi, không hiển thị cho user
        console.error('Error loading unread count:', error);
    }
}

// Cập nhật badge
function updateBadge(count) {
    const badge = document.getElementById('chatBadge');
    if (!badge) return;
    
    if (count > 0) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.classList.remove('hidden');
    } else {
        badge.classList.add('hidden');
    }
}

// Khởi tạo khi DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', createFloatingChatButton);
} else {
    createFloatingChatButton();
}

