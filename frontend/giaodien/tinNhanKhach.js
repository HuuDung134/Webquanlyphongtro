const API_URL = Config.API_URL;
const TIN_NHAN_ENDPOINT = Config.ENDPOINTS.TIN_NHAN;

// DOM Elements
const chatMessages = document.getElementById('chatMessages');
const messageForm = document.getElementById('messageForm');
const messageInput = document.getElementById('messageInput');
const userFullname = document.getElementById('userFullname');

// State
let messagePollInterval = null;
let isLoadingMessages = false;
let consecutiveErrors = 0;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    loadUserInfo();
    loadMessages();
    setupEventListeners();
    startMessagePolling();
});

// Check authentication
function checkAuth() {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    
    if (!token) {
        window.location.href = 'login.html';
        return;
    }
}

// Load user info
function loadUserInfo() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    userFullname.textContent = user.hoTen || user.tenDangNhap || 'Khách hàng';
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

// Load messages
async function loadMessages() {
    if (isLoadingMessages) return;
    
    isLoadingMessages = true;
    try {
        const data = await authFetch(`${TIN_NHAN_ENDPOINT}/khach/voi-admin?take=100`);
        const messages = data.danhSachTinNhan || [];
        displayMessages(messages);
        
        // Reset error count on success
        consecutiveErrors = 0;
        
        // Mark messages as read
        if (messages.length > 0) {
            try {
                await authFetch(`${TIN_NHAN_ENDPOINT}/khach/da-doc-tat-ca`, {
                    method: 'POST'
                });
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
        chatMessages.innerHTML = '<div class="text-center text-muted p-3">Chưa có tin nhắn nào. Hãy bắt đầu cuộc trò chuyện!</div>';
        return;
    }

    messages.forEach(msg => {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${msg.laKhachGui ? 'sent' : 'received'}`;
        
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
    
    const noiDung = messageInput.value.trim();
    if (!noiDung) return;
    
    // Disable input while sending
    const submitBtn = messageForm.querySelector('button[type="submit"]');
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gửi...';
    
    try {
        await authFetch(`${TIN_NHAN_ENDPOINT}/khach-gui-cho-admin`, {
            method: 'POST',
            body: JSON.stringify({
                noiDung: noiDung
            })
        });
        
        messageInput.value = '';
        
        // Reset error count on successful send
        consecutiveErrors = 0;
        
        // Small delay to ensure server processed the message
        await new Promise(resolve => setTimeout(resolve, 300));
        
        // Reload messages
        await loadMessages();
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
        loadMessages();
    }, 3000);
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

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (messagePollInterval) {
        clearInterval(messagePollInterval);
    }
});

