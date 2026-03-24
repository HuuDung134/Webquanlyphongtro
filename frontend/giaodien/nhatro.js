// DOM Elements
let dormitoryList;
let addDormitoryForm;
let saveDormitoryBtn;

// Initialize DOM elements
function initializeElements() {
    console.log('Initializing DOM elements...');
    dormitoryList = document.getElementById('dormitoryList');
    addDormitoryForm = document.getElementById('addDormitoryForm');
    saveDormitoryBtn = document.getElementById('saveDormitoryBtn');
    
    console.log('dormitoryList:', dormitoryList);
    console.log('addDormitoryForm:', addDormitoryForm);
    console.log('saveDormitoryBtn:', saveDormitoryBtn);
    
    if (!dormitoryList || !addDormitoryForm || !saveDormitoryBtn) {
        console.error('Required DOM elements not found');
        return false;
    }
    return true;
}

// Kiểm tra token trước khi load trang
function checkAuth() {
    const token = localStorage.getItem('token');
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
    
    console.log('Loading dormitories...');
    loadDormitories();
}

// Load dormitories when page loads
window.addEventListener('load', () => {
    console.log('Window loaded');
    console.log('Config:', window.Config);
    initializeApp();
});

// Load all dormitories
async function loadDormitories() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NHA_TRO}`, {
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
            throw new Error('Không thể tải danh sách nhà trọ');
        }

        const dormitories = await response.json();
        displayDormitories(dormitories);
    } catch (error) {
        console.error('Error loading dormitories:', error);
        showAlert(error.message || 'Không thể tải danh sách nhà trọ', 'danger');
    }
}

// Display dormitories in the UI
function displayDormitories(dormitories) {
    dormitoryList.innerHTML = '';
    dormitories.forEach(dormitory => {
        const dormitoryCard = createDormitoryCard(dormitory);
        dormitoryList.appendChild(dormitoryCard);
    });
}

// Create a dormitory card element
function createDormitoryCard(dormitory) {
    const col = document.createElement('div');
    col.className = 'col-md-4';
    
    col.innerHTML = `
        <div class="card dormitory-card">
            <div class="card-body">
                <i class="fas fa-building dormitory-icon"></i>
                <h5 class="card-title">${dormitory.tenNhaTro}</h5>
                <p class="card-text">
                    <strong>Địa chỉ:</strong> ${dormitory.diaChi}<br>
                </p>
                ${dormitory.moTa ? `<p class="card-text description-text">${dormitory.moTa}</p>` : ''}
                <div class="btn-group w-100">
                    <button class="btn btn-outline-primary" onclick="editDormitory(${dormitory.maNhaTro})">
                        <i class="fas fa-edit"></i> Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteDormitory(${dormitory.maNhaTro})">
                        <i class="fas fa-trash"></i> Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    
    return col;
}

async function saveDormitory() {
    const formData = new FormData(addDormitoryForm);
    const dormitoryData = {
        tenNhaTro: formData.get('tenNhaTro'),
        diaChi: formData.get('diaChi'),
        moTa: formData.get('moTa') || null
    };

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NHA_TRO}`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(dormitoryData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Lỗi khi thêm nhà trọ');
        }

        showAlert('Thêm nhà trọ thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addDormitoryModal')).hide();
        addDormitoryForm.reset();
        loadDormitories();
    } catch (error) {
        console.error('Error adding dormitory:', error);
        showAlert(error.message || 'Không thể thêm nhà trọ', 'danger');
    }
}

// Create mode setup
function createDormitory() {
    
    try {
        saveDormitoryBtn.onclick = () => saveDormitory();
    } catch (error) {
        console.error('Error loading service details:', error);
        showAlert('Không thể tải thông tin dịch vụ', 'danger');
    }
}
// Edit dormitory
async function editDormitory(dormitoryId) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NHA_TRO}/${dormitoryId}`, {
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
            const errorText = await response.text();
            throw new Error(errorText || 'Không thể tải thông tin nhà trọ');
        }

        const dormitory = await response.json();
        
        // Populate form with dormitory data
        addDormitoryForm.tenNhaTro.value = dormitory.tenNhaTro;
        addDormitoryForm.diaChi.value = dormitory.diaChi;
        addDormitoryForm.moTa.value = dormitory.moTa || '';

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('addDormitoryModal'));
        modal.show();
        
        // Update save button to handle edit
        saveDormitoryBtn.onclick = () => updateDormitory(dormitoryId);
    } catch (error) {
        console.error('Error loading dormitory details:', error);
        showAlert(error.message || 'Không thể tải thông tin nhà trọ', 'danger');
    }
}

// Update dormitory
async function updateDormitory(dormitoryId) {
    if (!addDormitoryForm.checkValidity()) {
        addDormitoryForm.reportValidity();
        return;
    }

    const formData = new FormData(addDormitoryForm);
    const dormitoryData = {
        maNhaTro: dormitoryId,
        tenNhaTro: formData.get('tenNhaTro'),
        diaChi: formData.get('diaChi'),
        moTa: formData.get('moTa') || null
    };

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NHA_TRO}/${dormitoryId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(dormitoryData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Lỗi khi cập nhật nhà trọ');
        }

        showAlert('Cập nhật nhà trọ thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addDormitoryModal')).hide();
        addDormitoryForm.reset();
        loadDormitories();
    } catch (error) {
        console.error('Error updating dormitory:', error);
        showAlert(error.message || 'Không thể cập nhật nhà trọ', 'danger');
    }
}

// Delete dormitory
async function deleteDormitory(dormitoryId) {
    if (!confirm('Bạn có chắc chắn muốn xóa nhà trọ này?')) return;

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NHA_TRO}/${dormitoryId}`, {
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
            if (errorText.includes('FK_Phong_NhaTro_MaNhaTro')) {
                throw new Error('Không thể xóa nhà trọ này vì đang có phòng đang sử dụng. Vui lòng xóa hoặc cập nhật các phòng liên quan trước.');
            }
            throw new Error(errorText || 'Lỗi khi xóa nhà trọ');
        }

        showAlert('Xóa nhà trọ thành công', 'success');
        loadDormitories();
    } catch (error) {
        console.error('Error deleting dormitory:', error);
        showAlert(error.message, 'danger');
    }
}

// Show alert message
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alertDiv);
    setTimeout(() => alertDiv.remove(), 3000);
} 