const API_URL = Config.API_URL;
const TIN_NHAN_ENDPOINT = Config.ENDPOINTS.TIN_NHAN;

// DOM Elements
const conversationList = document.getElementById('conversationList');
const chatMessages = document.getElementById('chatMessages');
const messageForm = document.getElementById('messageForm');
const messageInput = document.getElementById('messageInput');
const emptyChat = document.getElementById('emptyChat');
const chatContent = document.getElementById('chatContent');
const chatHeaderName = document.getElementById('chatHeaderName');
const chatHeaderPhone = document.getElementById('chatHeaderPhone');
const userFullname = document.getElementById('userFullname');

// State
let currentMaNguoiThue = null;
let conversations = [];
let messagePollInterval = null;
let isLoadingMessages = false;
let consecutiveErrors = 0;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    loadUserInfo();
    loadConversations();
    setupEventListeners();
});

// Check authentication
function checkAuth() {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    
    // Kiểm tra cả role và vaiTro để tương thích
    const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');
    
    if (!token || !isAdmin) {
        window.location.href = 'login.html';
        return;
    }
}

// Load user info
function loadUserInfo() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    // Tương thích với cả hai cách lưu: hoTen/fullname, tenDangNhap/username
    userFullname.textContent = user.hoTen || user.fullname || user.tenDangNhap || user.username || 'Admin';
}

// Setup event listeners
function setupEventListeners() {
    messageForm.addEventListener('submit', handleSendMessage);
}

// Helper function for API calls
async function authFetch(endpoint, options = {}) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        throw new Error('Chưa đăng nhập');
    }

    const defaultHeaders = {
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/json'
    };

    if (options.body && !(options.body instanceof FormData)) {
        defaultHeaders['Content-Type'] = 'application/json';
    }

    const config = {
        ...options,
        headers: {
            ...defaultHeaders,
            ...options.headers
        }
    };

    const response = await fetch(`${API_URL}${endpoint}`, config);

    if (response.status === 401) {
        localStorage.removeItem('token');
        window.location.href = 'login.html';
        throw new Error('Phiên đăng nhập hết hạn');
    }

    if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = 'Lỗi kết nối Server';
        try {
            const parsed = JSON.parse(errorText);
            errorMessage = parsed.thongBao || errorText || errorMessage;
        } catch {
            errorMessage = errorText || errorMessage;
        }
        throw new Error(errorMessage);
    }

    if (response.status === 204) return null;
    return response.json();
}

// Load conversations list
let isLoadingConversations = false;
async function loadConversations() {
    if (isLoadingConversations) return;
    
    isLoadingConversations = true;
    try {
        const data = await authFetch(`${TIN_NHAN_ENDPOINT}/admin/danh-sach-hoi-thoai`);
        conversations = data.danhSachHoiThoai || [];
        displayConversations();
    } catch (error) {
        console.error('Error loading conversations:', error);
        // Don't show alert for polling errors, only log
        // showAlert('Không thể tải danh sách hội thoại', 'danger');
    } finally {
        isLoadingConversations = false;
    }
}

// Display conversations
function displayConversations() {
    conversationList.innerHTML = '';
    
    if (conversations.length === 0) {
        conversationList.innerHTML = '<div class="p-3 text-center text-muted">Chưa có hội thoại nào</div>';
        return;
    }

    conversations.forEach(conv => {
        const item = document.createElement('div');
        item.className = 'conversation-item';
        if (conv.maNguoiThue === currentMaNguoiThue) {
            item.classList.add('active');
        }
        
        const timeStr = formatTime(conv.tinNhanCuoi.thoiGianGui);
        const preview = conv.tinNhanCuoi.noiDung.length > 50 
            ? conv.tinNhanCuoi.noiDung.substring(0, 50) + '...' 
            : conv.tinNhanCuoi.noiDung;
        
        item.innerHTML = `
            <div class="conversation-item-content">
                <div class="conversation-item-name">${conv.hoTen}</div>
                <div class="conversation-item-preview">${preview}</div>
                <div class="conversation-item-time">${timeStr}</div>
            </div>
            ${conv.soLuongChuaDoc > 0 ? `<span class="badge-unread">${conv.soLuongChuaDoc}</span>` : ''}
        `;
        
        item.addEventListener('click', () => selectConversation(conv.maNguoiThue, conv.hoTen, conv.soDienThoai));
        conversationList.appendChild(item);
    });
}

// Select conversation
function selectConversation(maNguoiThue, hoTen, soDienThoai) {
    currentMaNguoiThue = maNguoiThue;
    chatHeaderName.textContent = hoTen;
    chatHeaderPhone.textContent = soDienThoai || '';
    
    emptyChat.style.display = 'none';
    chatContent.style.display = 'flex';
    
    displayConversations(); // Update active state
    loadMessages();
    
    // Start polling for new messages
    startMessagePolling();
}

// Load messages
async function loadMessages() {
    if (!currentMaNguoiThue || isLoadingMessages) return;
    
    isLoadingMessages = true;
    try {
        const data = await authFetch(`${TIN_NHAN_ENDPOINT}/admin/khach-hang/${currentMaNguoiThue}?take=100`);
        const messages = data.danhSachTinNhan || [];
        displayMessages(messages);
        
        // Reset error count on success
        consecutiveErrors = 0;
        
        // Mark messages as read
        if (messages.length > 0) {
            try {
                await authFetch(`${TIN_NHAN_ENDPOINT}/admin/khach-hang/${currentMaNguoiThue}/da-doc-tat-ca`, {
                    method: 'POST'
                });
                // Reload conversations to update unread count
                loadConversations();
            } catch (markReadError) {
                // Silently fail marking as read, don't block message loading
                console.warn('Error marking messages as read:', markReadError);
            }
        }
    } catch (error) {
        consecutiveErrors++;
        console.error('Error loading messages:', error);
        
        // Only show alert for first error or critical errors (401, 500)
        const isCriticalError = error.message.includes('401') || error.message.includes('500') || error.message.includes('Phiên đăng nhập');
        if (consecutiveErrors === 1 || isCriticalError) {
            showAlert('Không thể tải tin nhắn', 'danger');
        }
        
        // If too many consecutive errors, stop polling
        if (consecutiveErrors >= 5) {
            console.warn('Too many consecutive errors, stopping polling');
            if (messagePollInterval) {
                clearInterval(messagePollInterval);
                messagePollInterval = null;
            }
        }
    } finally {
        isLoadingMessages = false;
    }
}

// Display messages
function displayMessages(messages) {
    chatMessages.innerHTML = '';
    
    if (messages.length === 0) {
        chatMessages.innerHTML = '<div class="text-center text-muted p-3">Chưa có tin nhắn nào</div>';
        return;
    }

    messages.forEach(msg => {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${msg.laAdminGui ? 'sent' : 'received'}`;
        
        const timeStr = formatDateTime(msg.thoiGianGui);
        
        messageDiv.innerHTML = `
            <div class="message-bubble">${escapeHtml(msg.noiDung)}</div>
            <div class="message-time">${timeStr}</div>
        `;
        
        chatMessages.appendChild(messageDiv);
    });
    
    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// Handle send message
async function handleSendMessage(e) {
    e.preventDefault();
    
    if (!currentMaNguoiThue) {
        showAlert('Vui lòng chọn một cuộc hội thoại', 'warning');
        return;
    }
    
    const noiDung = messageInput.value.trim();
    if (!noiDung) return;
    
    // Disable input while sending
    const submitBtn = messageForm.querySelector('button[type="submit"]');
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gửi...';
    
    try {
        await authFetch(`${TIN_NHAN_ENDPOINT}/admin-gui-cho-khach`, {
            method: 'POST',
            body: JSON.stringify({
                maNguoiNhan: currentMaNguoiThue,
                noiDung: noiDung
            })
        });
        
        messageInput.value = '';
        
        // Reset error count on successful send
        consecutiveErrors = 0;
        
        // Small delay to ensure server processed the message
        await new Promise(resolve => setTimeout(resolve, 300));
        
        // Reload messages and conversations
        await loadMessages();
        loadConversations();
    } catch (error) {
        console.error('Error sending message:', error);
        showAlert(error.message || 'Không thể gửi tin nhắn', 'danger');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnText;
    }
}

// Start polling for new messages
function startMessagePolling() {
    // Clear existing interval
    if (messagePollInterval) {
        clearInterval(messagePollInterval);
    }
    
    // Reset error count when starting new polling
    consecutiveErrors = 0;
    
    // Poll every 3 seconds
    messagePollInterval = setInterval(() => {
        if (currentMaNguoiThue) {
            loadMessages();
            // Only load conversations every 10 seconds to reduce load
            if (!messagePollInterval || Math.random() < 0.3) {
                loadConversations();
            }
        }
    }, 3000);
}

// Format time
function formatTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    if (diffMins < 1) return 'Vừa xong';
    if (diffMins < 60) return `${diffMins} phút trước`;
    if (diffHours < 24) return `${diffHours} giờ trước`;
    if (diffDays < 7) return `${diffDays} ngày trước`;
    
    return date.toLocaleDateString('vi-VN');
}

// Format date time
function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Show alert
function showAlert(message, type = 'info') {
    // Simple alert for now
    alert(message);
}

// Logout
function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = 'login.html';
}

