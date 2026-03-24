// Message Actions - Thu hồi và sửa tin nhắn
const API_URL = typeof Config !== 'undefined' ? Config.API_URL : '';
const TIN_NHAN_ENDPOINT = typeof Config !== 'undefined' && Config.ENDPOINTS ? Config.ENDPOINTS.TIN_NHAN : '/api/TinNhan';

// Helper function for API calls
async function authFetch(endpoint, options = {}) {
    const token = localStorage.getItem('token');
    if (!token) {
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

// Kiểm tra xem có thể sửa tin nhắn không (trong vòng 5 phút)
function canEditMessage(thoiGianGui) {
    const sentTime = new Date(thoiGianGui);
    const now = new Date();
    const diffMinutes = (now - sentTime) / (1000 * 60);
    return diffMinutes <= 5;
}

// Xóa tin nhắn (thu hồi = xóa)
async function recallMessage(messageId, reloadCallback) {
    if (!confirm('Bạn có chắc muốn xóa tin nhắn này?')) {
        return;
    }
    
    try {
        await authFetch(`${TIN_NHAN_ENDPOINT}/thu-hoi/${messageId}`, {
            method: 'POST'
        });
        
        if (reloadCallback) {
            reloadCallback();
        } else {
            location.reload();
        }
    } catch (error) {
        console.error('Error deleting message:', error);
        alert(error.message || 'Không thể xóa tin nhắn');
    }
}

// Sửa tin nhắn
async function editMessage(messageId, currentContent, reloadCallback) {
    const newContent = prompt('Sửa tin nhắn:', currentContent);
    if (!newContent || newContent.trim() === '' || newContent === currentContent) {
        return;
    }
    
    try {
        await authFetch(`${TIN_NHAN_ENDPOINT}/sua/${messageId}`, {
            method: 'PUT',
            body: JSON.stringify({
                noiDung: newContent.trim()
            })
        });
        
        if (reloadCallback) {
            reloadCallback();
        } else {
            location.reload();
        }
    } catch (error) {
        console.error('Error editing message:', error);
        alert(error.message || 'Không thể sửa tin nhắn');
    }
}

// Tạo HTML cho tin nhắn với nút hành động
function createMessageHTML(msg, isSent, reloadCallback) {
    const timeStr = formatDateTime(msg.thoiGianGui);
    const isRecalled = msg.daThuHoi || false;
    const isEdited = msg.daSua || false;
    const canEdit = isSent && !isRecalled && canEditMessage(msg.thoiGianGui);
    
    // Nội dung tin nhắn
    let messageContent = '';
    if (isRecalled) {
        messageContent = '<span style="font-style: italic; opacity: 0.7;">Tin nhắn đã được thu hồi</span>';
    } else {
        messageContent = escapeHtml(msg.noiDung || '');
        if (isEdited) {
            messageContent += ' <span style="font-size: 0.75rem; opacity: 0.7;">(đã chỉnh sửa)</span>';
        }
    }
    
    // Nút hành động
    let actionButtons = '';
    if (isSent && !isRecalled && canEdit) {
        const escapedContent = (msg.noiDung || '').replace(/'/g, "\\'").replace(/"/g, '&quot;');
        actionButtons = `
            <div class="message-actions" style="display: none;">
                <button class="btn-edit-message" onclick="editMessage(${msg.id}, '${escapedContent}', ${reloadCallback ? '() => { ' + reloadCallback.name + '(); }' : 'null'})" title="Sửa">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn-recall-message" onclick="recallMessage(${msg.id}, ${reloadCallback ? '() => { ' + reloadCallback.name + '(); }' : 'null'})" title="Thu hồi">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        `;
    }
    
    return {
        html: `
            <div class="message-bubble ${isRecalled ? 'recalled' : ''}" style="position: relative;">
                ${messageContent}
                ${actionButtons}
            </div>
            <div class="message-time">${timeStr}</div>
        `,
        canEdit: canEdit && isSent && !isRecalled
    };
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

// Export to window
window.recallMessage = recallMessage;
window.editMessage = editMessage;
window.canEditMessage = canEditMessage;
window.createMessageHTML = createMessageHTML;

