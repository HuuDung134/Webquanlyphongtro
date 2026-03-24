// API endpoints
const API_URL = Config.API_URL;

// DOM Elements
const serviceList = document.getElementById('serviceList');
const addServiceForm = document.getElementById('addServiceForm');
const saveServiceBtn = document.getElementById('saveServiceBtn');

// Load services when page loads
document.addEventListener('DOMContentLoaded', loadServices);

// Load all services
async function loadServices() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.DICH_VU}`, {
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
            throw new Error(errorData.message || 'Không thể tải danh sách dịch vụ');
        }

        const services = await response.json();
        if (!services || services.length === 0) {
            serviceList.innerHTML = '<div class="col-12 text-center"><p>Không có dịch vụ nào</p></div>';
            return;
        }
        displayServices(services);
    } catch (error) {
        console.error('Error loading services:', error);
        showAlert(error.message || 'Không thể tải danh sách dịch vụ', 'danger');
    }
}

// Display services in the UI
function displayServices(services) {
    serviceList.innerHTML = '';
    services.forEach(service => {
        const serviceCard = createServiceCard(service);
        serviceList.appendChild(serviceCard);
    });
}

// Create a service card element
function createServiceCard(service) {
    const col = document.createElement('div');
    col.className = 'col-md-4 mb-3';
    
    col.innerHTML = `
        <div class="card service-card h-100">
            <div class="card-body">
                <h5 class="card-title">${service.tenDichVu}</h5>
                <p class="card-text">
                    <strong>Giá dịch vụ:</strong> ${formatCurrency(service.tiendichvu)}
                </p>
                <div class="btn-group w-100">
                    <button class="btn btn-outline-primary" onclick="editService(${service.maDichVu})">
                        <i class="fas fa-edit"></i> Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteService(${service.maDichVu})">
                        <i class="fas fa-trash"></i> Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    
    return col;
}

// Format currency
function formatCurrency(amount) {
    if (isNaN(amount)) return '0 VNĐ';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Create new service
function createservice() {
    const modal = new bootstrap.Modal(document.getElementById('addServiceModal'));
    addServiceForm.reset();
    saveServiceBtn.onclick = saveservice;
    modal.show();
}

// Save new service
async function saveservice() {
    try {
        const formData = new FormData(addServiceForm);
        const tenDichVu = formData.get('tenDichVu');
        const tiendichvu = formData.get('tiendichvu');

        // Kiểm tra dữ liệu đầu vào
        if (!tenDichVu || !tiendichvu) {
            showAlert('Vui lòng nhập đầy đủ thông tin', 'danger');
            return;
        }

        // Kiểm tra độ dài tên dịch vụ
        if (tenDichVu.trim().length > 100) {
            showAlert('Tên dịch vụ không được vượt quá 100 ký tự', 'danger');
            return;
        }

        // Kiểm tra giá dịch vụ phải là số và lớn hơn 0
        const giaDichVu = parseFloat(tiendichvu);
        if (isNaN(giaDichVu) || giaDichVu <= 0) {
            showAlert('Giá dịch vụ phải là số và lớn hơn 0', 'danger');
            return;
        }

        // Kiểm tra giá dịch vụ không vượt quá giới hạn
        if (giaDichVu > 9999999999999) {
            showAlert('Giá dịch vụ không được vượt quá 9,999,999,999,999', 'danger');
            return;
        }

        // Gửi dữ liệu với ChiTietHoaDon là mảng rỗng
        const serviceData = {
            tenDichVu: tenDichVu.trim(),
            tiendichvu: giaDichVu,
            chiTietHoaDon: [] // Thêm mảng rỗng cho ChiTietHoaDon
        };

        console.log('Sending data:', serviceData);

        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.DICH_VU}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(serviceData)
        });

        console.log('Response status:', response.status);

        const responseData = await response.json();
        console.log('Response data:', responseData);

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            // Log chi tiết lỗi
            console.error('Server error:', {
                status: response.status,
                statusText: response.statusText,
                data: responseData
            });

            // Xử lý lỗi validation
            if (response.status === 400 && responseData.errors) {
                const errorMessages = Object.values(responseData.errors)
                    .flat()
                    .join('\n');
                throw new Error(errorMessages);
            }

            throw new Error(responseData.message || responseData.title || 'Lỗi khi thêm dịch vụ');
        }

        showAlert('Thêm dịch vụ thành công', 'success');
        
        // Đóng modal và xóa backdrop
        const modal = document.getElementById('addServiceModal');
        const modalInstance = bootstrap.Modal.getInstance(modal);
        if (modalInstance) {
            modalInstance.hide();
            // Xóa backdrop sau khi modal đã ẩn
            setTimeout(() => {
                const backdrop = document.querySelector('.modal-backdrop');
                if (backdrop) {
                    backdrop.remove();
                }
                document.body.classList.remove('modal-open');
                document.body.style.overflow = '';
                document.body.style.paddingRight = '';
            }, 150);
        }
        
        addServiceForm.reset();
        await loadServices();
    } catch (error) {
        console.error('Error adding service:', error);
        showAlert(error.message || 'Không thể thêm dịch vụ', 'danger');
    }
}

// Edit service
async function editService(serviceId) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.DICH_VU}/${serviceId}`, {
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
            throw new Error(errorData.message || 'Không thể tải thông tin dịch vụ');
        }

        const service = await response.json();
        console.log('Dữ liệu dịch vụ:', service);
        
        // Populate form with service data
        addServiceForm.tenDichVu.value = service.tenDichVu;
        addServiceForm.tiendichvu.value = service.tiendichvu;

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('addServiceModal'));
        modal.show();
        
        // Update save button to handle edit
        saveServiceBtn.onclick = () => updateService(serviceId);
    } catch (error) {
        console.error('Error loading service details:', error);
        showAlert(error.message || 'Không thể tải thông tin dịch vụ', 'danger');
    }
}

// Update service
async function updateService(serviceId) {
    try {
        const formData = new FormData(addServiceForm);
        const tenDichVu = formData.get('tenDichVu');
        const tiendichvu = formData.get('tiendichvu');

        // Validate dữ liệu trước khi gửi
        if (!tenDichVu || tenDichVu.trim() === '') {
            showAlert('Vui lòng nhập tên dịch vụ', 'danger');
            return;
        }

        if (!tiendichvu || isNaN(parseFloat(tiendichvu)) || parseFloat(tiendichvu) <= 0) {
            showAlert('Vui lòng nhập giá dịch vụ hợp lệ', 'danger');
            return;
        }

        const serviceData = {
            maDichVu: parseInt(serviceId),
            tenDichVu: tenDichVu.trim(),
            tiendichvu: parseFloat(tiendichvu)
        };

        console.log('Dữ liệu gửi lên:', serviceData);

        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.DICH_VU}/${serviceId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(serviceData)
        });

        console.log('Response status:', response.status);

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorData = await response.json();
            console.error('Chi tiết lỗi từ server:', errorData);
            
            // Xử lý lỗi validation từ server
            if (response.status === 400 && errorData.errors) {
                const errorMessages = Object.values(errorData.errors)
                    .flat()
                    .join('\n');
                throw new Error(errorMessages);
            }
            
            throw new Error(errorData.message || errorData.title || 'Lỗi khi cập nhật dịch vụ');
        }

        showAlert('Cập nhật dịch vụ thành công', 'success');
        
        // Đóng modal và xóa backdrop
        const modal = document.getElementById('addServiceModal');
        const modalInstance = bootstrap.Modal.getInstance(modal);
        if (modalInstance) {
            modalInstance.hide();
            // Xóa backdrop sau khi modal đã ẩn
            setTimeout(() => {
                const backdrop = document.querySelector('.modal-backdrop');
                if (backdrop) {
                    backdrop.remove();
                }
                document.body.classList.remove('modal-open');
                document.body.style.overflow = '';
                document.body.style.paddingRight = '';
            }, 150);
        }
        
        addServiceForm.reset();
        await loadServices();
    } catch (error) {
        console.error('Error updating service:', error);
        showAlert(error.message || 'Không thể cập nhật dịch vụ', 'danger');
    }
}

// Delete service
async function deleteService(serviceId) {
    if (!confirm('Bạn có chắc chắn muốn xóa dịch vụ này?')) return;

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.DICH_VU}/${serviceId}`, {
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
            throw new Error(errorData.message || 'Lỗi khi xóa dịch vụ');
        }

        showAlert('Xóa dịch vụ thành công', 'success');
        await loadServices();
    } catch (error) {
        console.error('Error deleting service:', error);
        showAlert(error.message || 'Không thể xóa dịch vụ', 'danger');
    }
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