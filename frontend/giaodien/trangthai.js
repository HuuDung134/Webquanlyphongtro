// DOM Elements
const trangThaiList = document.getElementById('trangThaiList');
const addTrangThaiForm = document.getElementById('addTrangThaiForm');
const saveTrangThaibtn = document.getElementById('saveTrangThaibtn');

// Kiểm tra token trước khi load trang
function checkAuth() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return false;
    }
    return true;
}

// Load statuses when page loads
document.addEventListener('DOMContentLoaded', () => {
    if (!checkAuth()) return;
    loadTrangThai();
});

// Load all statuses
async function loadTrangThai() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}/api/TrangThai`, {
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
            const errorData = await response.json();
            throw new Error(errorData.message || 'Không thể tải danh sách trạng thái');
        }

        const trangThai = await response.json();
        displayTrangThai(trangThai);
    } catch (error) {
        console.error('Error loading trạng thái:', error);
        showAlert(error.message || 'Không thể tải danh sách trạng thái', 'danger');
    }
}

// Display statuses in the UI
function displayTrangThai(trangThai) {
    trangThaiList.innerHTML = '';
    trangThai.forEach(item => {
        const card = createTrangThaiCard(item);
        trangThaiList.appendChild(card);
    });
}

// Create a status card element
function createTrangThaiCard(trangThai) {
    const col = document.createElement('div');
    col.className = 'col-md-4';
    col.innerHTML = `
        <div class="card trangThai-card">
            <div class="card-body">
                <h5 class="card-title">${trangThai.tenTrangThai}</h5>
               
                <div class="btn-group w-100">
                    <button class="btn btn-outline-primary" onclick="editTrangThai(${trangThai.maTrangThai})">
                        <i class="fas fa-edit"></i> Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteTrangThai(${trangThai.maTrangThai})">
                        <i class="fas fa-trash"></i> Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    return col;
}

// Save new status
async function saveTrangThai() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const formData = new FormData(addTrangThaiForm);
        const trangThaiData = {
            tenTrangThai: formData.get('tenTrangThai')
        };

        const response = await fetch(`${Config.API_URL}/api/TrangThai`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(trangThaiData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Lỗi khi thêm trạng thái');
        }

        showAlert('Thêm trạng thái thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addTrangThaiModal')).hide();
        addTrangThaiForm.reset();
        loadTrangThai();
    } catch (error) {
        console.error('Error saving trạng thái:', error);
        showAlert(error.message || 'Không thể thêm trạng thái', 'danger');
    }
}

// Create mode setup
function createTrangThai() {
    saveTrangThaibtn.onclick = () => saveTrangThai();
    addTrangThaiForm.reset();
    const modal = new bootstrap.Modal(document.getElementById('addTrangThaiModal'));
    modal.show();
}

// Edit status
async function editTrangThai(maTrangThai) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}/api/TrangThai/${maTrangThai}`, {
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
            const errorData = await response.json();
            throw new Error(errorData.message || 'Không thể tải thông tin trạng thái');
        }

        const trangThai = await response.json();
        
        addTrangThaiForm.tenTrangThai.value = trangThai.tenTrangThai;
        
        const modal = new bootstrap.Modal(document.getElementById('addTrangThaiModal'));
        modal.show();
        saveTrangThaibtn.onclick = () => updateTrangThai(maTrangThai);
    } catch (error) {
        console.error('Error loading trạng thái:', error);
        showAlert(error.message || 'Không thể tải thông tin trạng thái', 'danger');
    }
}

// Update status
async function updateTrangThai(maTrangThai) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const formData = new FormData(addTrangThaiForm);
        const trangThaiData = {
            maTrangThai: maTrangThai,
            tenTrangThai: formData.get('tenTrangThai')
        };
        
        const response = await fetch(`${Config.API_URL}/api/TrangThai/${maTrangThai}`, {
            method: 'PUT',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(trangThaiData)
        });
        
        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Lỗi khi cập nhật trạng thái');
        }

        showAlert('Cập nhật trạng thái thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addTrangThaiModal')).hide();
        addTrangThaiForm.reset();
        loadTrangThai();
    } catch (error) {
        console.error('Error updating trạng thái:', error);
        showAlert(error.message || 'Không thể cập nhật trạng thái', 'danger');
    }
}

// Delete status
async function deleteTrangThai(maTrangThai) {
    if (!confirm('Bạn có chắc chắn muốn xóa trạng thái này?')) return;

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}/api/TrangThai/${maTrangThai}`, {
            method: 'DELETE',
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
            const errorData = await response.json();
            throw new Error(errorData.message || 'Lỗi khi xóa trạng thái');
        }

        showAlert('Xóa trạng thái thành công', 'success');
        loadTrangThai();
    } catch (error) {
        console.error('Error deleting trạng thái:', error);
        showAlert(error.message || 'Không thể xóa trạng thái', 'danger');
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


