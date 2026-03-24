// Chat Modal - Bong bóng chat
// Đảm bảo Config đã được load
if (typeof Config === 'undefined') {
    console.error('Config.js chưa được load. Vui lòng đảm bảo Config.js được load trước chat-modal.js');
}

// Sử dụng function để tránh conflict khi load nhiều lần
function getChatAPI_URL() {
    return typeof Config !== 'undefined' ? Config.API_URL : '';
}

function getChatTIN_NHAN_ENDPOINT() {
    return typeof Config !== 'undefined' && Config.ENDPOINTS ? Config.ENDPOINTS.TIN_NHAN : '/api/TinNhan';
}

let chatModal = null;
let currentMaNguoiThue = null; // Cho admin
let messagePollInterval = null;
let isLoadingMessages = false;
let conversations = []; // Cho admin

// Tạo modal chat
function createChatModal() {
    if (document.getElementById('chatModalOverlay')) {
        return; // Đã tồn tại
    }

    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');

    const overlay = document.createElement('div');
    overlay.id = 'chatModalOverlay';
    overlay.className = 'chat-modal-overlay';
    
    const container = document.createElement('div');
    container.className = 'chat-modal-container';
    
    // Header
    const header = document.createElement('div');
    header.className = 'chat-modal-header';
    header.id = 'chatModalHeader';
    header.innerHTML = `
        ${isAdmin ? '<button class="chat-modal-back" id="chatModalBackBtn" onclick="goBackToConversations()" style="display: none;"><i class="fas fa-arrow-left"></i></button>' : ''}
        <div style="flex: 1;">
            <h5 id="chatModalTitle">${isAdmin ? 'Tin nhắn' : 'Tin nhắn với Admin'}</h5>
            <small id="chatModalSubtitle">${isAdmin ? 'Chọn cuộc hội thoại' : 'Gửi tin nhắn cho chủ trọ'}</small>
        </div>
        <button class="chat-modal-close" onclick="closeChatModal()">
            <i class="fas fa-times"></i>
        </button>
    `;
    
    // Body
    const body = document.createElement('div');
    body.className = 'chat-modal-body';
    body.id = 'chatModalBody';
    
    if (isAdmin) {
        // Admin: hiển thị danh sách hội thoại
        body.innerHTML = '<div id="chatModalConversationList" class="chat-modal-conversation-list"></div>';
    } else {
        // User: hiển thị tin nhắn
        body.innerHTML = '<div id="chatModalMessages"></div>';
    }
    
    // Footer - Form nhập tin nhắn (mặc định ẩn, chỉ hiển thị khi vào cuộc hội thoại)
    const footer = document.createElement('div');
    footer.className = 'chat-modal-footer';
    footer.id = 'chatModalFooter';
    footer.style.display = 'none'; // Mặc định ẩn
    footer.innerHTML = `
        <form id="chatModalForm" class="d-flex gap-2">
            <input type="text" class="form-control" id="chatModalInput" placeholder="Nhập tin nhắn..." required>
            <button type="submit" class="btn btn-primary">
                <i class="fas fa-paper-plane"></i> Gửi
            </button>
        </form>
    `;
    
    container.appendChild(header);
    container.appendChild(body);
    container.appendChild(footer);
    overlay.appendChild(container);
    document.body.appendChild(overlay);
    
    chatModal = overlay;
    
    // Event listeners - đợi một chút để DOM sẵn sàng
    setTimeout(() => {
        const form = document.getElementById('chatModalForm');
        if (form) {
            form.addEventListener('submit', handleSendMessage);
        }
        
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                closeChatModal();
            }
        });
        
        // Load data
        if (isAdmin) {
            // Ẩn footer khi đang ở danh sách hội thoại
            const footer = document.getElementById('chatModalFooter');
            if (footer) {
                footer.style.display = 'none';
            }
            loadConversations();
        } else {
            // Khách hàng: luôn hiển thị footer
            const footer = document.getElementById('chatModalFooter');
            if (footer) {
                footer.style.display = 'block';
            }
            loadMessages();
            startMessagePolling();
        }
    }, 100);
}

// Mở modal
function openChatModal() {
    try {
        console.log('=== openChatModal called ===');
        
        // Kiểm tra xem modal đã tồn tại chưa
        let modal = document.getElementById('chatModalOverlay');
        
        if (!modal) {
            console.log('Modal chưa tồn tại, đang tạo mới...');
            createChatModal();
            // Đợi một chút để DOM được cập nhật
            setTimeout(() => {
                modal = document.getElementById('chatModalOverlay');
                if (modal) {
                    showModal(modal);
                } else {
                    console.error('Không thể tạo modal');
                    alert('Không thể tạo chat modal. Vui lòng refresh trang.');
                }
            }, 100);
        } else {
            console.log('Modal đã tồn tại, đang hiển thị...');
            showModal(modal);
        }
    } catch (error) {
        console.error('Lỗi khi mở chat modal:', error);
        alert('Lỗi khi mở chat: ' + error.message);
    }
}

// Helper function để hiển thị modal
function showModal(modal) {
    if (!modal) return;
    
    console.log('Showing modal, current classes:', modal.className);
    modal.classList.add('show');
    document.body.style.overflow = 'hidden';
    chatModal = modal;
    
    // Force display nếu cần
    setTimeout(() => {
        const isVisible = modal.classList.contains('show');
        console.log('Modal visible after 100ms:', isVisible);
        if (!isVisible || window.getComputedStyle(modal).display === 'none') {
            console.warn('Modal không hiển thị, force display...');
            modal.style.display = 'flex';
            modal.style.alignItems = 'flex-end';
            modal.style.justifyContent = 'flex-end';
            modal.style.padding = '20px';
        }
    }, 100);
}

// Đóng modal
function closeChatModal() {
    if (chatModal) {
        chatModal.classList.remove('show');
        document.body.style.overflow = '';
        if (messagePollInterval) {
            clearInterval(messagePollInterval);
            messagePollInterval = null;
        }
    }
}

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

    const response = await fetch(`${getChatAPI_URL()}${endpoint}`, config);

    if (response.status === 401) {
        localStorage.removeItem('token');
        closeChatModal();
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

// Load conversations (Admin)
async function loadConversations() {
    try {
        const data = await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/admin/danh-sach-hoi-thoai`);
        conversations = data.danhSachHoiThoai || [];
        displayConversations();
    } catch (error) {
        console.error('Error loading conversations:', error);
    }
}

// Display conversations (Admin)
function displayConversations() {
    const list = document.getElementById('chatModalConversationList');
    if (!list) return;
    
    // Ẩn footer (form nhập tin) khi đang ở danh sách hội thoại
    const footer = document.getElementById('chatModalFooter');
    if (footer) {
        footer.style.display = 'none';
    }
    
    list.innerHTML = '';
    
    if (conversations.length === 0) {
        list.innerHTML = '<div class="chat-modal-empty"><i class="fas fa-comments"></i><p>Chưa có hội thoại nào</p></div>';
        return;
    }

    conversations.forEach(conv => {
        const item = document.createElement('div');
        item.className = 'chat-modal-conversation-item';
        if (conv.maNguoiThue === currentMaNguoiThue) {
            item.classList.add('active');
        }
        
        const timeStr = formatTime(conv.tinNhanCuoi.thoiGianGui);
        const preview = conv.tinNhanCuoi.noiDung.length > 40 
            ? conv.tinNhanCuoi.noiDung.substring(0, 40) + '...' 
            : conv.tinNhanCuoi.noiDung;
        
        item.innerHTML = `
            <div style="flex: 1; min-width: 0;">
                <div class="chat-modal-conversation-name">${conv.hoTen}</div>
                <div class="chat-modal-conversation-preview">${preview}</div>
                <div style="font-size: 0.75rem; color: #6c757d; margin-top: 0.25rem;">${timeStr}</div>
            </div>
            ${conv.soLuongChuaDoc > 0 ? `<span class="badge-unread" style="flex-shrink: 0;">${conv.soLuongChuaDoc}</span>` : ''}
        `;
        
        item.addEventListener('click', () => selectConversation(conv.maNguoiThue, conv.hoTen, conv.soDienThoai));
        list.appendChild(item);
    });
}

// Select conversation (Admin)
function selectConversation(maNguoiThue, hoTen, soDienThoai) {
    currentMaNguoiThue = maNguoiThue;
    document.getElementById('chatModalTitle').textContent = hoTen;
    document.getElementById('chatModalSubtitle').textContent = soDienThoai || '';
    
    // Hiển thị nút quay lại
    const backBtn = document.getElementById('chatModalBackBtn');
    if (backBtn) {
        backBtn.style.display = 'flex';
    }
    
    // Hiển thị footer (form nhập tin) khi đã chọn cuộc hội thoại
    const footer = document.getElementById('chatModalFooter');
    if (footer) {
        footer.style.display = 'block';
    }
    
    // Chuyển sang view tin nhắn
    const body = document.getElementById('chatModalBody');
    body.innerHTML = '<div id="chatModalMessages"></div>';
    
    displayConversations(); // Update active state
    loadMessages();
    startMessagePolling();
}

// Quay lại danh sách hội thoại (Admin)
function goBackToConversations() {
    currentMaNguoiThue = null;
    
    // Ẩn footer (form nhập tin) khi quay lại danh sách
    const footer = document.getElementById('chatModalFooter');
    if (footer) {
        footer.style.display = 'none';
    }
    
    // Ẩn nút quay lại
    const backBtn = document.getElementById('chatModalBackBtn');
    if (backBtn) {
        backBtn.style.display = 'none';
    }
    
    // Cập nhật header
    document.getElementById('chatModalTitle').textContent = 'Tin nhắn';
    document.getElementById('chatModalSubtitle').textContent = 'Chọn cuộc hội thoại';
    
    // Chuyển về view danh sách hội thoại
    const body = document.getElementById('chatModalBody');
    body.innerHTML = '<div id="chatModalConversationList" class="chat-modal-conversation-list"></div>';
    
    // Dừng polling
    if (messagePollInterval) {
        clearInterval(messagePollInterval);
        messagePollInterval = null;
    }
    
    // Load lại danh sách hội thoại
    loadConversations();
}

// Load messages
async function loadMessages() {
    if (isLoadingMessages) return;
    
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');
    
    isLoadingMessages = true;
    try {
        let data;
        if (isAdmin) {
            if (!currentMaNguoiThue) return;
            data = await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/admin/khach-hang/${currentMaNguoiThue}?take=100`);
        } else {
            data = await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/khach/voi-admin?take=100`);
        }
        
        const messages = data.danhSachTinNhan || [];
        displayMessages(messages);
        
        // Mark as read
        if (messages.length > 0) {
            try {
                if (isAdmin) {
                    await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/admin/khach-hang/${currentMaNguoiThue}/da-doc-tat-ca`, {
                        method: 'POST'
                    });
                } else {
                    await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/khach/da-doc-tat-ca`, {
                        method: 'POST'
                    });
                }
            } catch (e) {
                console.warn('Error marking as read:', e);
            }
        }
    } catch (error) {
        console.error('Error loading messages:', error);
    } finally {
        isLoadingMessages = false;
    }
}

// Display messages
function displayMessages(messages) {
    const container = document.getElementById('chatModalMessages');
    if (!container) return;
    
    container.innerHTML = '';
    
    if (messages.length === 0) {
        container.innerHTML = '<div class="chat-modal-empty"><i class="fas fa-comments"></i><p>Chưa có tin nhắn nào</p></div>';
        return;
    }

    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');

    messages.forEach(msg => {
        const messageDiv = document.createElement('div');
        const isSent = isAdmin ? msg.laAdminGui : msg.laKhachGui;
        messageDiv.className = `chat-modal-message ${isSent ? 'sent' : 'received'}`;
        messageDiv.dataset.messageId = msg.id;
        
        const timeStr = formatDateTime(msg.thoiGianGui);
        const isRecalled = msg.daThuHoi || false;
        const isEdited = msg.daSua || false;
        
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
        
        // Nút Xóa ở góc trên tin nhắn (chỉ cho tin nhắn của mình, chưa thu hồi)
        let actionButtons = '';
        if (isSent && !isRecalled) {
            // Chỉ hiển thị nút Xóa (backend sẽ kiểm tra quyền và thời gian)
            actionButtons = `
                <div class="message-action-buttons">
                    <button class="message-action-btn message-delete-btn" 
                            data-message-id="${msg.id}"
                            title="Xóa">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            `;
        }
        
        // Hiển thị tên người gửi: "Bạn" cho tin nhắn của mình, tên thật cho tin nhắn nhận được
        let senderName = '';
        if (isSent) {
            senderName = '<div class="chat-modal-message-sender">Bạn</div>';
        } else if (msg.tenNguoiGui) {
            senderName = `<div class="chat-modal-message-sender">${escapeHtml(msg.tenNguoiGui)}</div>`;
        }
        
        messageDiv.innerHTML = `
            ${senderName}
            <div class="chat-modal-message-bubble ${isRecalled ? 'recalled' : ''}" style="position: relative;">
                ${actionButtons}
                ${messageContent}
            </div>
            <div class="chat-modal-message-time">${timeStr}</div>
        `;
        
        // Thêm event listener cho nút Xóa
        if (isSent && !isRecalled) {
            const deleteBtn = messageDiv.querySelector('.message-delete-btn');
            
            if (deleteBtn) {
                deleteBtn.addEventListener('click', (e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    recallMessage(parseInt(deleteBtn.getAttribute('data-message-id')));
                });
            }
        }
        
        container.appendChild(messageDiv);
    });
    
    // Scroll to bottom
    container.scrollTop = container.scrollHeight;
}

// Handle send message
async function handleSendMessage(e) {
    e.preventDefault();
    
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');
    
    if (isAdmin && !currentMaNguoiThue) {
        alert('Vui lòng chọn một cuộc hội thoại');
        return;
    }
    
    const input = document.getElementById('chatModalInput');
    const noiDung = input.value.trim();
    if (!noiDung) return;
    
    const submitBtn = document.querySelector('#chatModalForm button[type="submit"]');
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    
    try {
        if (isAdmin) {
            await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/admin-gui-cho-khach`, {
                method: 'POST',
                body: JSON.stringify({
                    maNguoiNhan: currentMaNguoiThue,
                    noiDung: noiDung
                })
            });
        } else {
            await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/khach-gui-cho-admin`, {
                method: 'POST',
                body: JSON.stringify({
                    noiDung: noiDung
                })
            });
        }
        
        input.value = '';
        await new Promise(resolve => setTimeout(resolve, 300));
        await loadMessages();
        if (isAdmin) {
            loadConversations();
        }
    } catch (error) {
        console.error('Error sending message:', error);
        alert(error.message || 'Không thể gửi tin nhắn');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnText;
    }
}

// Start polling
function startMessagePolling() {
    if (messagePollInterval) {
        clearInterval(messagePollInterval);
    }
    messagePollInterval = setInterval(() => {
        loadMessages();
    }, 3000);
}

// Format time
function formatTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Vừa xong';
    if (diffMins < 60) return `${diffMins} phút trước`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)} giờ trước`;
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

// Export functions
// Hiển thị thông báo (toast notification)
function showChatNotification(message, type = 'info') {
    // Xóa thông báo cũ nếu có
    const existingNotification = document.getElementById('chatNotification');
    if (existingNotification) {
        existingNotification.remove();
    }
    
    const bgClass = type === 'success' ? 'bg-success' : (type === 'danger' ? 'bg-danger' : 'bg-info');
    const icon = type === 'success' ? 'fa-check-circle' : (type === 'danger' ? 'fa-times-circle' : 'fa-info-circle');
    
    const notification = document.createElement('div');
    notification.id = 'chatNotification';
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 10000; min-width: 300px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);';
    notification.innerHTML = `
        <i class="fas ${icon} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    document.body.appendChild(notification);
    
    // Tự động ẩn sau 4 giây
    setTimeout(() => {
        if (notification.parentNode) {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }
    }, 4000);
}

// Xóa tin nhắn (thu hồi = xóa)
async function recallMessage(messageId) {
    if (!confirm('Bạn có chắc muốn xóa tin nhắn này?')) {
        return;
    }
    
    try {
        await authFetch(`${getChatTIN_NHAN_ENDPOINT()}/thu-hoi/${messageId}`, {
            method: 'DELETE'
        });
        
        // Hiển thị thông báo thành công
        showChatNotification('Đã xóa tin nhắn thành công', 'success');
        
        // Reload messages để cập nhật danh sách (tin nhắn đã bị xóa)
        const user = JSON.parse(localStorage.getItem('user') || '{}');
        const isAdmin = (user.role === 'Admin' || user.vaiTro === 'Admin' || user.role === 'admin' || user.vaiTro === 'admin');
        
        if (isAdmin && currentMaNguoiThue) {
            // Nếu đang xem tin nhắn với một khách hàng, reload tin nhắn
            await loadMessages();
            // Cũng reload danh sách hội thoại để cập nhật tin nhắn cuối
            await loadConversations();
        } else {
            // Nếu là khách, chỉ reload tin nhắn
            await loadMessages();
        }
    } catch (error) {
        console.error('Error deleting message:', error);
        // Hiển thị thông báo lỗi đẹp hơn
        showChatNotification(error.message || 'Không thể xóa tin nhắn', 'danger');
    }
}

// Hiển thị context menu (chỉ có nút Xóa)
function showMessageContextMenu(event, msg) {
    // Đóng menu cũ nếu có
    const oldMenu = document.querySelector('.message-context-menu.show');
    const oldOverlay = document.querySelector('.message-context-menu-overlay.show');
    if (oldMenu) {
        oldMenu.classList.remove('show');
    }
    if (oldOverlay) {
        oldOverlay.classList.remove('show');
    }
    
    // Tạo overlay
    let overlay = document.getElementById('messageContextMenuOverlay');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = 'messageContextMenuOverlay';
        overlay.className = 'message-context-menu-overlay';
        document.body.appendChild(overlay);
    }
    
    // Tạo menu mới
    let menu = document.getElementById('messageContextMenu');
    if (!menu) {
        menu = document.createElement('div');
        menu.id = 'messageContextMenu';
        menu.className = 'message-context-menu';
        document.body.appendChild(menu);
    }
    
    menu.innerHTML = `
        <button class="message-context-menu-item danger" data-action="recall" data-message-id="${msg.id}">
            <i class="fas fa-trash"></i>
            <span>Xóa</span>
        </button>
    `;
    
    // Vị trí menu - hiển thị ở giữa màn hình như trong hình
    const menuWidth = 180;
    const menuHeight = 100;
    
    // Tính toán vị trí để menu ở giữa màn hình
    const left = (window.innerWidth - menuWidth) / 2;
    const top = (window.innerHeight - menuHeight) / 2;
    
    menu.style.left = left + 'px';
    menu.style.top = top + 'px';
    menu.style.pointerEvents = 'auto';
    
    // Hiển thị overlay và menu
    overlay.classList.add('show');
    menu.classList.add('show');
    
    // Event listeners cho menu items - dùng cả click và mousedown
    menu.querySelectorAll('.message-context-menu-item').forEach((item, index) => {
        // Xóa tất cả listeners cũ
        const newItem = item.cloneNode(true);
        item.parentNode.replaceChild(newItem, item);
        
        // Thêm click handler
        newItem.addEventListener('click', function(e) {
            e.stopPropagation();
            e.preventDefault();
            
            console.log('Menu item clicked:', this);
            
            const action = this.getAttribute('data-action');
            const messageId = parseInt(this.getAttribute('data-message-id'));
            
            if (this.disabled) {
                console.log('Item is disabled');
                return;
            }
            
            console.log('Executing action:', action, 'for message:', messageId);
            
            // Đóng menu và overlay
            const overlay = document.getElementById('messageContextMenuOverlay');
            if (overlay) overlay.classList.remove('show');
            menu.classList.remove('show');
            
            // Thực hiện action
            if (action === 'recall') {
                setTimeout(() => {
                    recallMessage(messageId);
                }, 100);
            }
        });
        
        // Thêm mousedown handler để đảm bảo
        newItem.addEventListener('mousedown', function(e) {
            e.stopPropagation();
            if (!this.disabled) {
                this.click();
            }
        });
    });
    
    // Đóng menu khi click vào overlay hoặc ra ngoài
    const closeMenu = (e) => {
        const overlay = document.getElementById('messageContextMenuOverlay');
        if (overlay && (overlay === e.target || (menu && !menu.contains(e.target) && !event.target.contains(e.target)))) {
            if (overlay) overlay.classList.remove('show');
            if (menu) menu.classList.remove('show');
            document.removeEventListener('mousedown', closeMenu);
            document.removeEventListener('click', closeMenu);
        }
    };
    
    // Click vào overlay để đóng
    overlay.addEventListener('click', (e) => {
        if (e.target === overlay) {
            overlay.classList.remove('show');
            menu.classList.remove('show');
        }
    });
    
    // Đợi một chút để menu render xong rồi mới thêm listener đóng
    setTimeout(() => {
        const closeHandler = (e) => {
            if (menu && !menu.contains(e.target) && overlay && e.target !== overlay) {
                overlay.classList.remove('show');
                menu.classList.remove('show');
                document.removeEventListener('click', closeHandler);
            }
        };
        document.addEventListener('click', closeHandler);
    }, 100);
}

// Tạo floating notification button ở góc trên bên phải
function createFloatingNotificationButton() {
    // Kiểm tra xem đã tồn tại chưa
    if (document.getElementById('floatingNotificationButton')) {
        return;
    }
    
    const token = localStorage.getItem('token');
    if (!token) return;
    
    // Tạo button
    const button = document.createElement('button');
    button.id = 'floatingNotificationButton';
    button.className = 'floating-notification-button';
    button.innerHTML = `
        <i class="fas fa-bell"></i>
        <span class="floating-notification-badge" id="floatingNotificationBadge" style="display: none;">0</span>
    `;
    button.title = 'Thông báo hệ thống';
    
    // Tạo panel
    const panel = document.createElement('div');
    panel.className = 'floating-notification-panel';
    panel.id = 'floatingNotificationPanel';
    panel.style.display = 'none';
    panel.innerHTML = `
        <div class="floating-notification-header">
            <h6>Thông báo hệ thống</h6>
            <button class="floating-notification-close" onclick="toggleFloatingNotificationPanel()">
                <i class="fas fa-times"></i>
            </button>
        </div>
        <div class="floating-notification-list" id="floatingNotificationList">
            <div class="floating-notification-loading">Đang tải...</div>
        </div>
    `;
    
    button.addEventListener('click', (e) => {
        e.stopPropagation();
        toggleFloatingNotificationPanel();
    });
    
    document.body.appendChild(button);
    document.body.appendChild(panel);
    
    // Load notifications khi tạo button
    setTimeout(() => loadFloatingNotifications(), 200);
}

// Toggle notification panel
function toggleFloatingNotificationPanel() {
    const panel = document.getElementById('floatingNotificationPanel');
    if (!panel) return;
    
    if (panel.style.display === 'none') {
        panel.style.display = 'block';
        loadFloatingNotifications();
        // Ẩn badge khi mở panel
        const badge = document.getElementById('floatingNotificationBadge');
        if (badge) {
            badge.style.display = 'none';
        }
    } else {
        panel.style.display = 'none';
    }
}

// Load notifications cho floating button
async function loadFloatingNotifications() {
    const list = document.getElementById('floatingNotificationList');
    if (!list) return;
    
    try {
        const token = localStorage.getItem('token');
        
        const response = await fetch(`${getChatAPI_URL()}/api/ThongBao`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            list.innerHTML = '<div class="floating-notification-empty">Không thể tải thông báo</div>';
            return;
        }
        
        const notifications = await response.json();
        
        if (notifications.length === 0) {
            list.innerHTML = '<div class="floating-notification-empty"><i class="fas fa-bell-slash"></i><p>Chưa có thông báo nào</p></div>';
            // Ẩn badge
            const badge = document.getElementById('floatingNotificationBadge');
            if (badge) badge.style.display = 'none';
            return;
        }
        
        list.innerHTML = notifications.map(notif => {
            const createdDate = notif.createdAt || notif.CreatedAt;
            const timeStr = formatDateTime(createdDate);
            const title = escapeHtml(notif.title || notif.Title || 'Thông báo');
            const content = escapeHtml(notif.content || notif.Content || '');
            
            return `
                <div class="floating-notification-item">
                    <div class="floating-notification-title">${title}</div>
                    <div class="floating-notification-content">${content}</div>
                    <div class="floating-notification-time">${timeStr}</div>
                </div>
            `;
        }).join('');
        
        // Cập nhật badge - chỉ hiển thị nếu panel đang đóng
        const badge = document.getElementById('floatingNotificationBadge');
        const panel = document.getElementById('floatingNotificationPanel');
        if (badge) {
            const unreadCount = notifications.length;
            // Chỉ hiển thị badge nếu có thông báo VÀ panel đang đóng
            if (unreadCount > 0 && panel && panel.style.display === 'none') {
                badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
                badge.style.display = 'flex';
            } else {
                badge.style.display = 'none';
            }
        }
    } catch (error) {
        console.error('Error loading notifications:', error);
        list.innerHTML = '<div class="floating-notification-empty">Lỗi khi tải thông báo</div>';
    }
}

// Export functions to window
window.openChatModal = openChatModal;
window.closeChatModal = closeChatModal;
window.goBackToConversations = goBackToConversations;
window.toggleFloatingNotificationPanel = toggleFloatingNotificationPanel;
window.recallMessage = recallMessage;

// Tạo floating notification button khi load
// Đợi một chút để đảm bảo DOM và các script khác đã load xong
setTimeout(() => {
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', createFloatingNotificationButton);
    } else {
        createFloatingNotificationButton();
    }
}, 500);

// Debug
console.log('chat-modal.js loaded successfully');
console.log('openChatModal available:', typeof window.openChatModal);

