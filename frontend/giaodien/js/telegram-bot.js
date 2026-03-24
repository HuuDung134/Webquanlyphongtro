// Telegram Bot Widget
class TelegramBotWidget {
    constructor(baseUrl) {
        this.baseUrl = baseUrl || 'http://localhost:5253';
        this.chatId = null;
        this.isOpen = false;
        this.init();
    }

    init() {
        this.createWidget();
        this.loadBotInfo();
        this.setupEventListeners();
    }

    createWidget() {
        const widget = document.createElement('div');
        widget.className = 'telegram-bot-widget';
        widget.innerHTML = `
            <div class="telegram-bot-chat" id="telegramBotChat">
                <div class="telegram-bot-header">
                    <div>
                        <h5>🤖 Hỗ trợ trực tuyến (AI)</h5>
                        <small id="telegramBotStatus" class="telegram-bot-status">Đang kết nối...</small>
                    </div>
                    <button class="close-btn" id="telegramBotClose">×</button>
                </div>
                <div class="telegram-bot-messages" id="telegramBotMessages">
                    <div class="telegram-bot-message bot">
                        <div id="telegramBotWelcomeMessage">Đang tải...</div>
                        <div class="telegram-bot-message-time">${this.getCurrentTime()}</div>
                    </div>
                </div>
                <div class="telegram-bot-quick-buttons" id="telegramBotQuickButtons">
                    <!-- Quick buttons sẽ được thêm động dựa trên vai trò -->
                </div>
                <div class="telegram-bot-input-area">
                    <input type="text" class="telegram-bot-input" id="telegramBotInput" placeholder="Nhập tin nhắn hoặc chọn nút bên trên...">
                    <button class="telegram-bot-send-btn" id="telegramBotSend">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <line x1="22" y1="2" x2="11" y2="13"></line>
                            <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
                        </svg>
                    </button>
                </div>
            </div>
            <button class="telegram-bot-button" id="telegramBotButton">
                <svg viewBox="0 0 24 24" fill="currentColor">
                    <path d="M12 0C5.373 0 0 5.373 0 12s5.373 12 12 12 12-5.373 12-12S18.627 0 12 0zm5.562 8.161c-.178 1.994-1.006 6.816-1.417 9.03-.17.92-.504 1.226-.827 1.255-.7.06-1.23-.461-1.908-.904-1.06-.695-1.66-1.128-2.69-1.805-1.19-.81-.42-1.255.26-1.98.18-.19 3.243-2.976 3.303-3.23.007-.03.014-.15-.055-.212-.07-.062-.17-.037-.243-.022-.104.023-1.75 1.11-4.94 3.258-.467.31-.89.46-1.27.453-.418-.008-1.223-.235-1.82-.43-.733-.24-1.315-.367-1.264-.775.025-.2.375-.405 1.033-.616 4.04-1.75 6.73-2.9 8.07-3.45 3.88-1.62 4.68-1.9 5.2-1.92.118-.004.38-.008.55.28.13.22.09.38.052.53-.038.15-.064.24-.104.37z"/>
                </svg>
            </button>
        `;
        document.body.appendChild(widget);
    }

    setupEventListeners() {
        const button = document.getElementById('telegramBotButton');
        const closeBtn = document.getElementById('telegramBotClose');
        const sendBtn = document.getElementById('telegramBotSend');
        const input = document.getElementById('telegramBotInput');

        button.addEventListener('click', () => this.toggleChat());
        closeBtn.addEventListener('click', () => this.closeChat());
        sendBtn.addEventListener('click', () => this.sendMessage());
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendMessage();
            }
        });
    }

    toggleChat() {
        const chat = document.getElementById('telegramBotChat');
        this.isOpen = !this.isOpen;
        if (this.isOpen) {
            chat.classList.add('active');
            document.getElementById('telegramBotInput').focus();
        } else {
            chat.classList.remove('active');
        }
    }

    closeChat() {
        const chat = document.getElementById('telegramBotChat');
        this.isOpen = false;
        chat.classList.remove('active');
    }

    async loadBotInfo() {
        try {
            const response = await fetch(`${this.baseUrl}/api/TelegramBot/bot-info`);
            const data = await response.json();
            const statusEl = document.getElementById('telegramBotStatus');
            
            if (data.configured) {
                if (statusEl) {
                    statusEl.textContent = `@${data.username}`;
                    statusEl.className = 'telegram-bot-status online';
                }
                console.log('Telegram Bot đã sẵn sàng:', data.username);
                
                // Load quick buttons dựa trên vai trò user
                await this.loadQuickButtons();
            } else {
                if (statusEl) {
                    statusEl.textContent = 'Chưa kết nối (Bot tạm tắt)';
                    statusEl.className = 'telegram-bot-status offline';
                }
            }
        } catch (error) {
            console.error('Không thể kết nối với Telegram Bot:', error);
            const statusEl = document.getElementById('telegramBotStatus');
            if (statusEl) {
                statusEl.textContent = 'Lỗi kết nối (Bot tạm tắt)';
                statusEl.className = 'telegram-bot-status offline';
            }
        }
    }

    async loadQuickButtons() {
        try {
            const token = localStorage.getItem('token');
            if (!token) return;

            // Lấy thông tin user để xác định vai trò
            const userStr = localStorage.getItem('user');
            if (!userStr) return;

            let user;
            try {
                user = JSON.parse(userStr);
            } catch (e) {
                console.error('Error parsing user data:', e);
                return;
            }

            const isLandlord = user.role === 'Admin' || user.role === 'admin';
            const quickButtonsContainer = document.getElementById('telegramBotQuickButtons');
            const welcomeMessageEl = document.getElementById('telegramBotWelcomeMessage');
            
            if (!quickButtonsContainer) return;

            // Cập nhật welcome message dựa trên vai trò
            if (welcomeMessageEl) {
                if (isLandlord) {
                    welcomeMessageEl.innerHTML = `👋 Xin chào Chủ trọ! Tôi là trợ lý ảo báo cáo của bạn.<br><br>💡 Tôi có thể giúp bạn:<br>• 📊 Báo cáo doanh thu nhanh<br>• 🏠 Xem phòng trống<br>• 🔔 Nhắc nợ tự động<br>• 🔍 Tra cứu thông tin khách thuê<br><br>Chọn nút bên dưới hoặc hỏi tôi bất cứ điều gì!`;
                } else {
                    welcomeMessageEl.innerHTML = `👋 Xin chào! Tôi là lễ tân 24/7 của bạn.<br><br>💡 Tôi có thể giúp bạn:<br>• 💸 Tra cứu hóa đơn theo phòng<br>• 📋 Xem thông tin hợp đồng<br>• 🛠️ Báo cáo sự cố<br>• 📶 Hỏi đáp tự động (WiFi, giờ đóng cửa...)<br><br>Chọn nút bên dưới hoặc hỏi tôi bất cứ điều gì!`;
                }
            }

            if (isLandlord) {
                // Quick buttons cho Chủ Trọ (Trợ lý ảo báo cáo)
                quickButtonsContainer.innerHTML = `
                    <button class="telegram-quick-btn" data-action="📊 Doanh thu tháng này thế nào?">📊 Doanh thu</button>
                    <button class="telegram-quick-btn" data-action="🏠 Còn bao nhiêu phòng trống?">🏠 Phòng trống</button>
                    <button class="telegram-quick-btn" data-action="🔔 Nhắc nợ tất cả phòng chưa đóng tiền">🔔 Nhắc nợ</button>
                    <button class="telegram-quick-btn" data-action="📊 Thống kê">📊 Thống kê</button>
                    <button class="telegram-quick-btn" data-action="🔍 Phòng 201 là ai thuê?">🔍 Tra cứu khách</button>
                    <button class="telegram-quick-btn" data-action="💳 Hóa đơn chưa thanh toán">💳 Công nợ</button>
                `;
            } else {
                // Quick buttons cho Khách Thuê (Lễ tân 24/7)
                quickButtonsContainer.innerHTML = `
                    <button class="telegram-quick-btn" data-action="💸 Tháng này phòng của tôi hết bao nhiêu tiền?">💸 Hóa đơn</button>
                    <button class="telegram-quick-btn" data-action="📋 Hợp đồng của tôi bao giờ hết hạn?">📋 Hợp đồng</button>
                    <button class="telegram-quick-btn" data-action="🛠️ Báo sự cố">🛠️ Báo sự cố</button>
                    <button class="telegram-quick-btn" data-action="📶 Pass Wifi là gì?">📶 Pass WiFi</button>
                    <button class="telegram-quick-btn" data-action="🔐 Giờ đóng cửa là mấy giờ?">🔐 Giờ đóng cửa</button>
                    <button class="telegram-quick-btn" data-action="🏠 Thông tin phòng của tôi">🏠 Thông tin phòng</button>
                `;
            }

            // Thêm event listeners cho quick buttons
            quickButtonsContainer.querySelectorAll('.telegram-quick-btn').forEach(btn => {
                btn.addEventListener('click', () => {
                    const url = btn.getAttribute('data-url');
                    if (url) {
                        window.open(url, '_blank');
                        return;
                    }
                    const action = btn.getAttribute('data-action');
                    document.getElementById('telegramBotInput').value = action;
                    this.sendMessage();
                });
            });
        } catch (error) {
            console.error('Lỗi khi load quick buttons:', error);
        }
    }

    async sendMessage() {
        const input = document.getElementById('telegramBotInput');
        const message = input.value.trim();
        
        if (!message) return;

        // Hiển thị tin nhắn của user
        this.addMessage(message, 'user');
        input.value = '';

        // Hiển thị typing indicator với thông báo AI đang xử lý
        this.showTyping();
        this.updateTypingMessage('AI đang suy nghĩ...');

        try {
            // Gửi tin nhắn đến backend để xử lý
            const token = localStorage.getItem('token');
            if (!token) {
                this.hideTyping();
                this.addMessage('Bạn cần đăng nhập để sử dụng tính năng này.', 'bot');
                return;
            }

            const response = await fetch(`${this.baseUrl}/api/TelegramBot/process-message`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ message: message })
            });

            this.hideTyping();

            if (!response.ok) {
                // Xử lý lỗi từ server
                if (response.status === 401) {
                    this.addMessage('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.', 'bot');
                    return;
                }
                
                let errorMessage = 'Có lỗi xảy ra. Vui lòng thử lại sau.';
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorData.response || errorMessage;
                } catch (e) {
                    // Nếu không parse được JSON, dùng message mặc định
                }
                this.addMessage(errorMessage, 'bot');
                return;
            }

            const data = await response.json();
            
            if (data.response) {
                this.addMessage(data.response, 'bot');
            } else if (data.message) {
                this.addMessage(data.message, 'bot');
            } else {
                this.addMessage('Xin lỗi, tôi không hiểu. Vui lòng thử lại hoặc liên hệ trực tiếp với chủ trọ.', 'bot');
            }
        } catch (error) {
            console.error('Lỗi khi gửi tin nhắn:', error);
            this.hideTyping();
            this.addMessage('Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng và thử lại.', 'bot');
        }
    }

    addMessage(text, type) {
        const messagesContainer = document.getElementById('telegramBotMessages');
        const messageDiv = document.createElement('div');
        messageDiv.className = `telegram-bot-message ${type}`;
        messageDiv.innerHTML = `
            <div>${this.formatMessage(text)}</div>
            <div class="telegram-bot-message-time">${this.getCurrentTime()}</div>
        `;
        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    formatMessage(text) {
        // Format markdown đơn giản và emoji, hỗ trợ bảng markdown
        let formatted = text
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/`(.*?)`/g, '<code>$1</code>')
            .replace(/\n/g, '<br>');

        // Format bảng markdown (| col1 | col2 |)
        formatted = formatted.replace(/\|(.+?)\|/g, (match, content) => {
            const cells = content.split('|').map(cell => cell.trim()).filter(cell => cell);
            if (cells.length > 0 && cells[0].includes('---')) {
                return ''; // Bỏ qua dòng separator
            }
            return '<span class="table-cell">' + cells.join('</span><span class="table-cell">') + '</span>';
        });

        // Format emoji
        formatted = formatted.replace(/(📊|🔔|🏠|💸|🛠️|🔑|🤖|⚠️|✅|❌|📱|💰|👥|📋|📶|🔐|🔍|📸|🛠|📋|💳)/g, '<span class="emoji">$1</span>');

        return formatted;
    }

    showTyping(message = 'Đang nhập...') {
        const messagesContainer = document.getElementById('telegramBotMessages');
        const typingDiv = document.createElement('div');
        typingDiv.id = 'telegramBotTyping';
        typingDiv.className = 'telegram-bot-typing';
        typingDiv.innerHTML = `
            <div class="typing-indicator">
                <span></span><span></span><span></span>
            </div>
            <div class="typing-message">${message}</div>
        `;
        messagesContainer.appendChild(typingDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    updateTypingMessage(message) {
        const typingDiv = document.getElementById('telegramBotTyping');
        if (typingDiv) {
            const messageEl = typingDiv.querySelector('.typing-message');
            if (messageEl) {
                messageEl.textContent = message;
            }
        }
    }

    hideTyping() {
        const typingDiv = document.getElementById('telegramBotTyping');
        if (typingDiv) {
            typingDiv.remove();
        }
    }

    getCurrentTime() {
        const now = new Date();
        return now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }
}

// Khởi tạo widget khi trang được tải
document.addEventListener('DOMContentLoaded', () => {
    // Kiểm tra xem đã đăng nhập chưa
    const token = localStorage.getItem('token');
    if (!token) {
        return; // Không hiển thị bot nếu chưa đăng nhập
    }

    // Lấy baseUrl từ config hoặc localStorage
    let baseUrl = 'http://localhost:5253';
    
    // Thử lấy từ window.Config (user-dashboard.html)
    if (window.Config && window.Config.API_URL) {
        baseUrl = window.Config.API_URL.replace('/api', '');
    }
    // Thử lấy từ module (index.html)
    else if (typeof baseUrl !== 'undefined') {
        // baseUrl đã được import từ module
    }
    
    window.telegramBotWidget = new TelegramBotWidget(baseUrl);
});

