// API endpoint
const API_URL = Config.API_URL;

// DOM Elements
const tenantList = document.getElementById('tenantList');
const addTenantForm = document.getElementById('addTenantForm');
const saveTenantBtn = document.getElementById('saveTenantBtn');
const searchInput = document.getElementById('searchInput');
const genderFilter = document.getElementById('genderFilter');
const nationalityFilter = document.getElementById('nationalityFilter');

// Current editing tenant ID
let currentEditingId = null;

// Mở modal chọn tài khoản
window.openSelectAccountModal = async function() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }
        // Lấy danh sách tài khoản
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.TAI_KHOAN}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        if (!response.ok) {
            throw new Error('Không thể tải danh sách tài khoản');
        }
        const accounts = await response.json();

        // Lấy danh sách người thuê để kiểm tra tài khoản đã gắn
        const tenantsRes = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NGUOI_THUE}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        const tenants = tenantsRes.ok ? await tenantsRes.json() : [];
        const usedAccountIds = tenants.filter(t => t.maNguoiDung).map(t => t.maNguoiDung);

        const tbody = document.querySelector('#accountsTable tbody');
        tbody.innerHTML = '';
        // Chỉ hiển thị tài khoản chưa được gắn cho bất kỳ người thuê nào
        accounts.forEach(account => {
            if (account.vaiTro === 'NguoiDung' && !usedAccountIds.includes(account.maNguoiDung)) {
                const row = `
                    <tr>
                        <td>${account.maNguoiDung}</td>
                        <td>${account.tenDangNhap}</td>
                        <td>
                            <button class="btn btn-sm btn-primary" onclick="selectAccount(${account.maNguoiDung}, '${account.tenDangNhap}')">
                                <i class="fas fa-check"></i> Chọn
                            </button>
                        </td>
                    </tr>
                `;
                tbody.innerHTML += row;
            }
        });
        const modal = new bootstrap.Modal(document.getElementById('selectAccountModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading accounts:', error);
        showAlert('Không thể tải danh sách tài khoản', 'danger');
    }
};

// Chọn tài khoản
window.selectAccount = function(maNguoiDung, hoTen) {
    document.getElementById('maNguoiDung').value = maNguoiDung;
    document.getElementById('selectedAccount').value = hoTen;
    bootstrap.Modal.getInstance(document.getElementById('selectAccountModal')).hide();
};

// Load thông tin tài khoản
async function loadAccountInfo(maNguoiDung) {
    if (!maNguoiDung) return;
    
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.TAI_KHOAN}/${maNguoiDung}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error('Không thể tải thông tin tài khoản');
        }

        const account = await response.json();
        document.getElementById('selectedAccount').value = account.tenDangNhap;
    } catch (error) {
        console.error('Error loading account info:', error);
        showAlert('Không thể tải thông tin tài khoản', 'danger');
    }
}

// Load tenants when page loads
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    loadThongBao();
    loadNguoiThue();
    setupEventListeners();
});

// Setup event listeners
function setupEventListeners() {
    // Search and filter events
    searchInput.addEventListener('input', filterNguoiThue);
    genderFilter.addEventListener('change', filterNguoiThue);
    nationalityFilter.addEventListener('change', filterNguoiThue);

    // Save button event
    saveTenantBtn.addEventListener('click', saveNguoiThue);
}

// Load all tenants
async function loadNguoiThue() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        console.log('Loading tenants from:', `${Config.API_URL}${Config.ENDPOINTS.NGUOI_THUE}`);
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NGUOI_THUE}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        console.log('Response status:', response.status);
        const responseText = await response.text();
        console.log('Response text:', responseText);

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error(`Lỗi khi tải danh sách người thuê: ${responseText}`);
        }

        let nguoiThue;
        try {
            nguoiThue = JSON.parse(responseText);
        } catch (e) {
            console.error('Error parsing response:', e);
            throw new Error('Dữ liệu trả về không hợp lệ');
        }

        console.log('Loaded tenants:', nguoiThue);
        await displayNguoiThue(nguoiThue);
    } catch (error) {
        console.error('Error loading tenants:', error);
        showAlert(error.message || 'Không thể tải danh sách người thuê', 'danger');
    }
}

// Display tenants in the UI
async function displayNguoiThue(nguoiThue) {
    tenantList.innerHTML = '';
    for (const tenant of nguoiThue) {
        const tenantCard = await createNguoiThueCard(tenant);
        tenantList.appendChild(tenantCard);
    }
}

// Create a tenant card element
async function createNguoiThueCard(tenant) {
    const col = document.createElement('div');
    col.className = 'col-md-6 col-lg-4 mb-4';
    
    let accountInfo = '';
    if (tenant.taiKhoan) {
        accountInfo = `
            <div class="mt-3 p-2 bg-light rounded">
                <p class="mb-0"><i class="fas fa-user me-2"></i>Tài khoản: ${tenant.taiKhoan.tenDangNhap}</p>
            </div>
        `;
    }
    
    col.innerHTML = `
        <div class="tenant-card">
            <div class="card-body">
                <div class="text-center mb-3">
                    <i class="fas fa-user-circle fa-3x text-primary"></i>
                    <h5 class="mt-2">${tenant.hoTen || ''}</h5>
                </div>
                <p><i class="fas fa-id-card me-2"></i>CCCD: ${tenant.cccd || ''}</p>
                <p><i class="fas fa-phone me-2"></i>SĐT: ${tenant.sdt || ''}</p>
                <p><i class="fas fa-envelope me-2"></i>Email: ${tenant.email || ''}</p>
                <p><i class="fas fa-calendar me-2"></i>Ngày sinh: ${formatDate(tenant.ngaySinh)}</p>
                <p><i class="fas fa-venus-mars me-2"></i>Giới tính: ${tenant.gioiTinh || ''}</p>
                <p><i class="fas fa-map-marker-alt me-2"></i>Địa chỉ: ${tenant.diaChi || ''}</p>
                <p><i class="fas fa-globe me-2"></i>Quốc tịch: ${tenant.quocTich || ''}</p>
                <p><i class="fas fa-building me-2"></i>Nơi công tác: ${tenant.noiCongTac || ''}</p>
                ${accountInfo}
                <div class="btn-group w-100 mt-3">
                    <button class="btn btn-outline-primary" onclick="editNguoiThue(${parseInt(tenant.maNguoiThue)})">
                        <i class="fas fa-edit me-2"></i>Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteNguoiThue(${parseInt(tenant.maNguoiThue)})">
                        <i class="fas fa-trash me-2"></i>Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    
    return col;
}

// Format date
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Create new tenant
function createNguoiThue() {
    currentEditingId = null;
    addTenantForm.reset();
    const modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
    modal.show();
}

// Save tenant (create or update)
async function saveNguoiThue() {
    if (!addTenantForm.checkValidity()) {
        addTenantForm.reportValidity();
        return;
    }
    const formData = new FormData(addTenantForm);
    const tenantData = {
        hoTen: formData.get('hoTen'),
        sdt: formData.get('sdt'),
        email: formData.get('email'),
        cccd: formData.get('cccd'),
        diaChi: formData.get('diaChi'),
        ngaySinh: formData.get('ngaySinh'),
        gioiTinh: formData.get('gioiTinh'),
        quocTich: formData.get('quocTich'),
        noiCongTac: formData.get('noiCongTac'),
        maNguoiDung: formData.get('maNguoiDung') || null
    };
    // Nếu đang cập nhật, thêm MaNguoiThue
    if (currentEditingId) {
        tenantData.maNguoiThue = currentEditingId;
    }
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }
        const url = currentEditingId 
            ? `${API_URL}${Config.ENDPOINTS.NGUOI_THUE}/${currentEditingId}`
            : `${API_URL}${Config.ENDPOINTS.NGUOI_THUE}`;
        const response = await fetch(url, {
            method: currentEditingId ? 'PUT' : 'POST',
            headers: { 
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json' 
            },
            body: JSON.stringify(tenantData)
        });
        if (!response.ok) {
            let errorMessage = 'Lỗi khi lưu thông tin người thuê';
            const responseText = await response.text();
            if (responseText.trim().startsWith('{')) {
                try {
                    const errorData = JSON.parse(responseText);
                    errorMessage = errorData.thongBao || errorMessage;
                } catch (e) {}
            } else {
                errorMessage = responseText || errorMessage;
            }
            throw new Error(errorMessage);
        }
        showAlert(currentEditingId ? 'Cập nhật người thuê thành công' : 'Thêm người thuê thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addTenantModal')).hide();
        addTenantForm.reset();
        loadNguoiThue();
    } catch (error) {
        showAlert(error.message || 'Không thể lưu thông tin người thuê', 'danger');
    }
}

// Edit tenant
async function editNguoiThue(id) {
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.NGUOI_THUE}/${id}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const tenant = await response.json();
        currentEditingId = id;
        addTenantForm.hoTen.value = tenant.hoTen || '';
        addTenantForm.sdt.value = tenant.sdt;
        addTenantForm.email.value = tenant.email || '';
        addTenantForm.cccd.value = tenant.cccd;
        addTenantForm.diaChi.value = tenant.diaChi;
        addTenantForm.ngaySinh.value = tenant.ngaySinh ? tenant.ngaySinh.split('T')[0] : '';
        addTenantForm.gioiTinh.value = tenant.gioiTinh;
        addTenantForm.quocTich.value = tenant.quocTich;
        addTenantForm.noiCongTac.value = tenant.noiCongTac;
        // Cập nhật thông tin tài khoản nếu có
        if (tenant.maNguoiDung) {
            document.getElementById('maNguoiDung').value = tenant.maNguoiDung;
            loadAccountInfo(tenant.maNguoiDung);
        } else {
            document.getElementById('maNguoiDung').value = '';
            document.getElementById('selectedAccount').value = '';
        }
        const modal = new bootstrap.Modal(document.getElementById('addTenantModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading tenant details:', error);
        showAlert('Không thể tải thông tin người thuê', 'danger');
    }
}

// Delete tenant
async function deleteNguoiThue(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa người thuê này?')) return;

    try {
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.NGUOI_THUE}/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Lỗi khi xóa người thuê');
        }

        showAlert('Xóa người thuê thành công', 'success');
        loadNguoiThue();
    } catch (error) {
        console.error('Error deleting tenant:', error);
        showAlert('Không thể xóa người thuê', 'danger');
    }
}

// Filter tenants
function filterNguoiThue() {
    const searchTerm = searchInput.value.toLowerCase();
    const selectedGender = genderFilter.value;
    const selectedNationality = nationalityFilter.value;

    const cards = tenantList.getElementsByClassName('col-md-6');
    
    Array.from(cards).forEach(card => {
        const tenantInfo = card.textContent.toLowerCase();
        const gender = card.querySelector('p:nth-child(6)').textContent.includes(selectedGender);
        const nationality = card.querySelector('p:nth-child(8)').textContent.includes(selectedNationality);
        
        const matchesSearch = searchTerm === '' || tenantInfo.includes(searchTerm);
        const matchesGender = selectedGender === '' || gender;
        const matchesNationality = selectedNationality === '' || nationality;
        
        card.style.display = matchesSearch && matchesGender && matchesNationality ? '' : 'none';
    });
}

// Reset filters
function resetFilters() {
    searchInput.value = '';
    genderFilter.value = '';
    nationalityFilter.value = '';
    filterNguoiThue();
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

async function loadThongBao() {
    try {
        const res = await fetch(`${API_URL}/api/thongbao`);
        const data = await res.json();
        const thongBaoDiv = document.getElementById('thongBaoList');
        thongBaoDiv.innerHTML = '';
        if (!data || data.length === 0) {
            return;
        }
        data.forEach(tb => {
            thongBaoDiv.innerHTML += `
                <div class="alert alert-warning alert-dismissible fade show mb-2" role="alert">
                    <strong>${tb.title}</strong>: ${tb.content}
                    <span class="badge bg-light text-dark ms-2">${new Date(tb.createdAt).toLocaleString('vi-VN')}</span>
                </div>
            `;
        });
    } catch (error) {
        console.error('Không thể tải thông báo hệ thống');
    }
}