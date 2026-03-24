// API endpoints
const API_URL = Config.API_URL;

// DOM Elements
const contractList = document.getElementById('contractList');
const addContractForm = document.getElementById('addContractForm');
const saveContractBtn = document.getElementById('saveContractBtn');

// Helper function to get select elements
function getTenantSelect() {
    return document.querySelector('select[name="maNguoiThue"]');
}

function getRoomSelect() {
    return document.querySelector('select[name="maPhong"]');
}

// Current editing contract ID
let currentEditingId = null;

// Load data when page loads
document.addEventListener('DOMContentLoaded', () => {
    loadContracts();
    setupEventListeners();
    
    // Load tenants and rooms when modal is shown
    const modal = document.getElementById('addContractModal');
    if (modal) {
        modal.addEventListener('show.bs.modal', () => {
            // Load dữ liệu khi modal được mở
            if (!currentEditingId) {
                // Nếu đang tạo mới, chỉ load phòng và người thuê chưa có hợp đồng
                loadRooms(false);
                loadTenants(false);
            }
        });
    }
});

// Setup event listeners
function setupEventListeners() {
    saveContractBtn.addEventListener('click', saveContract);
}

// Load all contracts
async function loadContracts() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.HOP_DONG}`, {
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
            throw new Error('Không thể tải danh sách hợp đồng');
        }

        const contracts = await response.json();
        displayContracts(contracts);
    } catch (error) {
        console.error('Error loading contracts:', error);
        showAlert('Không thể tải danh sách hợp đồng', 'danger');
    }
}

// Load tenants (load all tenants, not just those without contracts)
async function loadTenants(includeAll = false) {
    try {
        const tenantSelect = getTenantSelect();
        if (!tenantSelect) {
            console.warn('Tenant select element not found, skipping load');
            return;
        }

        const token = localStorage.getItem('token');
        let endpoint = `${API_URL}${Config.ENDPOINTS.HOP_DONG}/NguoiThue/KhongCoHopDong`;
        
        // Nếu cần load tất cả (khi edit), load từ endpoint NguoiThue
        if (includeAll) {
            endpoint = `${API_URL}${Config.ENDPOINTS.NGUOI_THUE || '/api/NguoiThue'}`;
        }
        
        const response = await fetch(endpoint, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('Error response:', errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }
        const tenants = await response.json();

        tenantSelect.innerHTML = '<option value="">Chọn người thuê...</option>';
        if (Array.isArray(tenants)) {
            tenants.forEach(tenant => {
                const option = document.createElement('option');
                option.value = tenant.maNguoiThue || tenant.MaNguoiThue;
                option.textContent = tenant.hoTen || tenant.HoTen;
                tenantSelect.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading tenants:', error);
        showAlert('Không thể tải danh sách người thuê: ' + error.message, 'danger');
    }
}

// Load rooms (load all rooms, not just those without contracts)
async function loadRooms(includeAll = false) {
    try {
        const roomSelect = getRoomSelect();
        if (!roomSelect) {
            console.warn('Room select element not found, skipping load');
            return;
        }

        const token = localStorage.getItem('token');
        let endpoint = `${API_URL}${Config.ENDPOINTS.HOP_DONG}/Phong/KhongCoHopDong`;
        
        // Nếu cần load tất cả (khi edit), load từ endpoint Phong
        if (includeAll) {
            endpoint = `${API_URL}${Config.ENDPOINTS.PHONG || '/api/Phong'}`;
        }
        
        const response = await fetch(endpoint, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Error response:', errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const rooms = await response.json();
        roomSelect.innerHTML = '<option value="">Chọn phòng...</option>';
        if (Array.isArray(rooms)) {
            rooms.forEach(room => {
                const option = document.createElement('option');
                option.value = room.maPhong || room.MaPhong;
                option.textContent = room.tenPhong || room.TenPhong;
                roomSelect.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading rooms:', error);
        showAlert('Không thể tải danh sách phòng: ' + error.message, 'danger');
    }
}

// Display contracts
function displayContracts(contracts) {
    contractList.innerHTML = '';
    contracts.forEach(contract => {
        const contractCard = createContractCard(contract);
        contractList.appendChild(contractCard);
    });
}

// Create contract card
function createContractCard(contract) {
    const col = document.createElement('div');
    col.className = 'col-md-6 col-lg-4 mb-4';

    const statusClass = getStatusClass(contract.ngayKetThuc);
    const statusText = contract.trangThaiText || getStatusText(contract.ngayKetThuc);

    col.innerHTML = `
        <div class="card contract-card">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h5 class="card-title mb-0">Hợp đồng #${contract.maHopDong}</h5>
                    <span class="badge ${statusClass}">${statusText}</span>
                </div>
                <p class="card-text">
                    <i class="fas fa-user me-2"></i>Người thuê: ${contract.nguoiThue.hoTen || 'N/A'}<br>
                    <i class="fas fa-door-open me-2"></i>Phòng: ${contract.phong.tenPhong || 'N/A'}<br>
                    <i class="fas fa-calendar-alt me-2"></i>Ngày bắt đầu: ${formatDate(contract.ngayBatDau)}<br>
                    <i class="fas fa-calendar-times me-2"></i>Ngày kết thúc: ${formatDate(contract.ngayKetThuc)}<br>
                    <i class="fas fa-money-bill-wave me-2"></i>Tiền cọc: ${formatCurrency(contract.tienCoc)}<br>
                    <i class="fas fa-sticky-note me-2"></i>Nội dung: ${contract.noiDung || 'Không có'}
                </p>
                <div class="btn-group w-100 mb-2">
                    <button class="btn btn-outline-primary" onclick="editContract(${contract.maHopDong})">
                        <i class="fas fa-edit me-2"></i>Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteContract(${contract.maHopDong})">
                        <i class="fas fa-trash me-2"></i>Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    return col;
}

// Helpers
function getStatusClass(endDate) {
    if (!endDate) return 'bg-success';
    const today = new Date();
    const end = new Date(endDate);
    if (end < today) return 'bg-danger';
    if (end - today < 7 * 24 * 60 * 60 * 1000) return 'bg-warning';
    return 'bg-success';
}

function getStatusText(endDate) {
    if (!endDate) return 'Đang còn hiệu lực';
    const today = new Date();
    const end = new Date(endDate);
    if (end < today) return 'Kết thúc hợp đồng';
    if (end - today < 7 * 24 * 60 * 60 * 1000) return 'Sắp hết hợp đồng';
    return 'Đang còn hiệu lực';
}

function formatDate(dateStr) {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString('vi-VN');
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

async function saveContract() {
    if (!addContractForm.checkValidity()) {
        addContractForm.reportValidity();
        return;
    }

    const formData = new FormData(addContractForm);
    const contractData = {
        MaHopDong: currentEditingId || 0,
        MaNguoiThue: parseInt(formData.get('maNguoiThue')),
        MaPhong: parseInt(formData.get('maPhong')),
        NgayBatDau: formData.get('ngayBatDau'),
        NgayKetThuc: formData.get('ngayKetThuc') || null,
        TienCoc: parseFloat(formData.get('tienCoc')),
        NoiDung: formData.get('noiDung') || null
    };

    if (contractData.ngayKetThuc && new Date(contractData.ngayKetThuc) < new Date(contractData.ngayBatDau)) {
        showAlert('Ngày kết thúc phải sau ngày bắt đầu', 'warning');
        return;
    }

    if (contractData.tienCoc < 0) {
        showAlert('Tiền cọc không được âm', 'warning');
        return;
    }

    saveContractBtn.disabled = true;

    try {
        const token = localStorage.getItem('token');
        const url = currentEditingId 
            ? `${API_URL}${Config.ENDPOINTS.HOP_DONG}/${currentEditingId}`
            : `${API_URL}${Config.ENDPOINTS.HOP_DONG}`;

        const response = await fetch(url, {
            method: currentEditingId ? 'PUT' : 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(contractData)
        });

        if (!response.ok) {
            const text = await response.text();
            let errorMsg = text;
            try {
                const errorData = JSON.parse(text);
                errorMsg = errorData.message || JSON.stringify(errorData);
            } catch {
                // Không phải JSON, giữ nguyên text
            }
            throw new Error(errorMsg);
        }

        showAlert(currentEditingId ? 'Cập nhật hợp đồng thành công' : 'Thêm hợp đồng thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addContractModal')).hide();
        addContractForm.reset();
        currentEditingId = null;
        loadContracts();
    } catch (error) {
        console.error('Error saving contract:', error);
        showAlert(error.message || 'Không thể lưu hợp đồng', 'danger');
    } finally {
        saveContractBtn.disabled = false;
    }
}

// Edit contract
async function editContract(id) {
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.HOP_DONG}/${id}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error('Không thể tải thông tin hợp đồng');
        }
        
        const contract = await response.json();
        
        // Lấy thông tin phòng và người thuê hiện tại
        const maNguoiThue = contract.maNguoiThue || contract.MaNguoiThue;
        const maPhong = contract.maPhong || contract.MaPhong;
        const tenNguoiThue = contract.nguoiThue?.hoTen || contract.nguoiThue?.HoTen || 'N/A';
        const tenPhong = contract.phong?.tenPhong || contract.phong?.TenPhong || 'N/A';
        
        // Load tất cả phòng và người thuê để có thể chỉnh sửa
        await Promise.all([
            loadRooms(true),  // Load tất cả phòng
            loadTenants(true) // Load tất cả người thuê
        ]);
        
        // Đợi một chút để đảm bảo select đã được populate
        await new Promise(resolve => setTimeout(resolve, 150));
        
        // Đảm bảo phòng và người thuê hiện tại có trong dropdown
        // Nếu chưa có, thêm vào
        const roomSelect = getRoomSelect();
        const tenantSelect = getTenantSelect();
        
        if (roomSelect && maPhong && !roomSelect.querySelector(`option[value="${maPhong}"]`)) {
            const option = document.createElement('option');
            option.value = maPhong;
            option.textContent = tenPhong;
            roomSelect.appendChild(option);
        }
        
        if (tenantSelect && maNguoiThue && !tenantSelect.querySelector(`option[value="${maNguoiThue}"]`)) {
            const option = document.createElement('option');
            option.value = maNguoiThue;
            option.textContent = tenNguoiThue;
            tenantSelect.appendChild(option);
        }
        
        currentEditingId = id;
        
        // Set giá trị cho select
        addContractForm.maNguoiThue.value = maNguoiThue;
        addContractForm.maPhong.value = maPhong;
        addContractForm.ngayBatDau.value = contract.ngayBatDau ? contract.ngayBatDau.split('T')[0] : '';
        addContractForm.ngayKetThuc.value = contract.ngayKetThuc ? contract.ngayKetThuc.split('T')[0] : '';
        addContractForm.tienCoc.value = contract.tienCoc || contract.TienCoc || 0;
        addContractForm.noiDung.value = contract.noiDung || contract.NoiDung || '';

        document.getElementById('addContractModalLabel').textContent = 'Sửa hợp đồng';
        const modal = new bootstrap.Modal(document.getElementById('addContractModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading contract details:', error);
        showAlert(error.message || 'Không thể tải thông tin hợp đồng', 'danger');
    }
}

// Delete contract
async function deleteContract(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa hợp đồng này?')) return;

    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.HOP_DONG}/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error('Lỗi khi xóa hợp đồng');
        }

        showAlert('Xóa hợp đồng thành công', 'success');
        loadContracts();
    } catch (error) {
        console.error('Error deleting contract:', error);
        showAlert('Không thể xóa hợp đồng', 'danger');
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

