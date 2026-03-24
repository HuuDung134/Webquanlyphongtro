// API endpoints
const API_URL = Config.API_URL;

// DOM Elements
let roomTypeList;
let addRoomTypeForm;
let saveRoomTypeBtn;

// Initialize DOM elements
function initializeElements() {
    console.log('Initializing DOM elements...');
    roomTypeList = document.getElementById('roomTypeList');
    addRoomTypeForm = document.getElementById('addRoomTypeForm');
    saveRoomTypeBtn = document.getElementById('saveRoomTypeBtn');
    
    console.log('roomTypeList:', roomTypeList);
    console.log('addRoomTypeForm:', addRoomTypeForm);
    console.log('saveRoomTypeBtn:', saveRoomTypeBtn);
    
    if (!roomTypeList || !addRoomTypeForm || !saveRoomTypeBtn) {
        console.error('Required DOM elements not found');
        return false;
    }
    return true;
}

// Kiểm tra token trước khi load trang
function checkAuth() {
    const token = localStorage.getItem('token');
    console.log('Token:', token ? 'Có token' : 'Không có token');
    if (!token) {
        window.location.href = 'login.html';
        return false;
    }
    return true;
}

// Initialize application
function initializeApp() {
    console.log('Initializing application...');
    if (!checkAuth()) {
        console.log('Auth check failed');
        return;
    }
    
    if (!initializeElements()) {
        console.log('DOM elements initialization failed');
        return;
    }
    
    console.log('Loading room types...');
    loadRoomTypes();
}

// Load room types when page loads
window.addEventListener('load', () => {
    console.log('Window loaded');
    console.log('Config:', window.Config);
    initializeApp();
});

// Load all room types
async function loadRoomTypes() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        // Kiểm tra Config
        if (!window.Config) {
            throw new Error('Config chưa được load');
        }

        const url = `${window.Config.API_URL}${window.Config.ENDPOINTS.LOAI_PHONG}`;
        console.log('Request URL:', url);
        console.log('Request Headers:', {
            'Authorization': `Bearer ${token}`,
            'Accept': 'application/json'
        });
        
        // Thử gọi API với fetch
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        console.log('Response status:', response.status);
        console.log('Response headers:', Object.fromEntries(response.headers.entries()));

        if (response.status === 401) {
            console.log('Unauthorized - Redirecting to login');
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            console.error('API Error Response:', errorText);
            throw new Error(`Lỗi ${response.status}: ${errorText || 'Không thể tải danh sách loại phòng'}`);
        }

        const roomTypes = await response.json();
        console.log('Loaded room types:', roomTypes);
        displayRoomTypes(roomTypes);
    } catch (error) {
        console.error('Error loading room types:', error);
        showAlert(error.message || 'Không thể tải danh sách loại phòng', 'danger');
    }
}

// Display room types in the UI
function displayRoomTypes(roomTypes) {
    console.log('Displaying room types:', roomTypes);
    if (!roomTypeList) {
        console.error('Room type list element not found');
        return;
    }
    
    roomTypeList.innerHTML = '';
    roomTypes.forEach(roomType => {
        const card = createRoomTypeCard(roomType);
        roomTypeList.appendChild(card);
    });
}

// Create a room type card element
function createRoomTypeCard(roomType) {
    const col = document.createElement('div');
    col.className = 'col-md-4';
    col.innerHTML = `
        <div class="card room-type-card">
            <div class="card-body">
                <h5 class="card-title">${roomType.tenLoaiPhong}</h5>
                <p class="card-text">
                    <strong>Mô tả:</strong> ${roomType.moTa || 'Không có'}
                </p>
                <div class="btn-group w-100">
                    <button class="btn btn-outline-primary" onclick="editRoomType(${roomType.maLoaiPhong})">
                        <i class="fas fa-edit"></i> Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteRoomType(${roomType.maLoaiPhong})">
                        <i class="fas fa-trash"></i> Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    return col;
}

// Save new room type
async function saveRoomType() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const formData = new FormData(addRoomTypeForm);
        const roomTypeData = {
            tenLoaiPhong: formData.get('tenLoaiPhong'),
            moTa: formData.get('moTa') || null
        };

        console.log('Saving room type:', roomTypeData);

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.LOAI_PHONG}`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(roomTypeData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            console.error('API Error:', errorText);
            throw new Error(errorText || 'Lỗi khi thêm loại phòng');
        }

        showAlert('Thêm loại phòng thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addRoomTypeModal')).hide();
        addRoomTypeForm.reset();
        loadRoomTypes();
    } catch (error) {
        console.error('Error adding room type:', error);
        showAlert(error.message || 'Không thể thêm loại phòng', 'danger');
    }
}

// Create mode setup
function createRoomType() {
    if (!saveRoomTypeBtn) return;
    
    saveRoomTypeBtn.onclick = () => saveRoomType();
    addRoomTypeForm.reset();
    const modal = new bootstrap.Modal(document.getElementById('addRoomTypeModal'));
    modal.show();
}

// Edit room type
async function editRoomType(roomTypeId) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.LOAI_PHONG}/${roomTypeId}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        
        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error('Không thể tải thông tin loại phòng');
        }
        
        const roomType = await response.json();

        addRoomTypeForm.tenLoaiPhong.value = roomType.tenLoaiPhong;
        addRoomTypeForm.moTa.value = roomType.moTa || '';

        document.getElementById('addRoomTypeModalLabel').textContent = 'Sửa loại phòng';
        const modal = new bootstrap.Modal(document.getElementById('addRoomTypeModal'));
        modal.show();

        saveRoomTypeBtn.onclick = () => updateRoomType(roomTypeId);
    } catch (error) {
        console.error('Error loading room type:', error);
        showAlert(error.message || 'Không thể tải thông tin loại phòng', 'danger');
    }
}

// Update room type
async function updateRoomType(roomTypeId) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const formData = new FormData(addRoomTypeForm);
        const roomTypeData = {
            maLoaiPhong: roomTypeId,
            tenLoaiPhong: formData.get('tenLoaiPhong'),
            moTa: formData.get('moTa') || null
        };

        console.log('Updating room type:', roomTypeData);

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.LOAI_PHONG}/${roomTypeId}`, {
            method: 'PUT',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(roomTypeData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            console.error('API Error:', errorText);
            throw new Error(errorText || 'Lỗi khi cập nhật loại phòng');
        }

        showAlert('Cập nhật loại phòng thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addRoomTypeModal')).hide();
        addRoomTypeForm.reset();
        loadRoomTypes();
    } catch (error) {
        console.error('Error updating room type:', error);
        showAlert(error.message || 'Không thể cập nhật loại phòng', 'danger');
    }
}

// Delete room type
async function deleteRoomType(roomTypeId) {
    if (!confirm('Bạn có chắc chắn muốn xóa loại phòng này?')) return;

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.LOAI_PHONG}/${roomTypeId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            console.error('API Error:', errorText);
            if (errorText.includes('FK_Phong_LoaiPhong_MaLoaiPhong')) {
                throw new Error('Không thể xóa loại phòng này vì đang có phòng đang sử dụng. Vui lòng xóa hoặc cập nhật các phòng liên quan trước.');
            }
            throw new Error(errorText || 'Lỗi khi xóa loại phòng');
        }

        showAlert('Xóa loại phòng thành công', 'success');
        loadRoomTypes();
    } catch (error) {
        console.error('Error deleting room type:', error);
        showAlert(error.message, 'danger');
    }
}

// Helper Functions
function formatCurrency(amount) {
    if (isNaN(amount) || amount === null || amount === undefined) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Show alert message
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    alertDiv.style.zIndex = '9999';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alertDiv);
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}
